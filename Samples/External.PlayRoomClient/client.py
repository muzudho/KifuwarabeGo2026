"""Transport-only Play Room v1 client. Python standard library, no project DLLs."""
import asyncio
import json
import sys
import uuid
from collections import deque


class ProtocolError(Exception):
    pass


class RemoteError(ProtocolError):
    def __init__(self, error):
        self.error = error
        super().__init__(f"{error['code']}: {error['message']}")


class Client:
    """One outstanding request; timeout or framing failure closes the process."""

    def __init__(self, command, timeout=5, trace=False):
        self.command, self.timeout, self.trace = command, timeout, trace
        self.process = None
        self.diagnostics = deque(maxlen=16)
        self._lock = asyncio.Lock()

    async def __aenter__(self):
        self.process = await asyncio.create_subprocess_exec(
            *self.command, stdin=asyncio.subprocess.PIPE,
            stdout=asyncio.subprocess.PIPE, stderr=asyncio.subprocess.PIPE,
            limit=1024 * 1024)
        self._stderr = asyncio.create_task(self._drain_stderr())
        return self

    async def _drain_stderr(self):
        while chunk := await self.process.stderr.read(4096):
            self.diagnostics.append(chunk.decode('utf-8', errors='replace'))

    async def __aexit__(self, *_):
        await self.close()

    async def close(self):
        if self.process is None:
            return
        if self.process.returncode is None:
            try:
                self.process.kill()
            except ProcessLookupError:
                pass
        # Unpause stdout after an oversized line so process.wait can complete.
        while await self.process.stdout.read(4096):
            pass
        await self.process.wait()
        await self._stderr

    async def finish(self):
        """Verify that a completion response is followed by a normal exit."""
        try:
            code = await asyncio.wait_for(self.process.wait(), self.timeout)
            if code != 0:
                raise ProtocolError(f'Host exit code: {code}')
        except BaseException:
            await self.close()
            raise

    async def request(self, method, parameters, version=1):
        async with self._lock:
            request_id = uuid.uuid4().hex
            request = dict(protocolVersion=version, requestId=request_id,
                           method=method, parameters=parameters)
            async def exchange():
                line = json.dumps(request, ensure_ascii=False)
                if self.trace:
                    print('> ' + line, file=sys.stderr)
                self.process.stdin.write((line + '\n').encode('utf-8'))
                await self.process.stdin.drain()
                line = await self.process.stdout.readline()
                if not line:
                    raise ProtocolError('Host exited before responding')
                response = json.loads(line.decode('utf-8'))
                if self.trace:
                    print('< ' + line.decode('utf-8').rstrip(), file=sys.stderr)
                if not isinstance(response, dict):
                    raise ProtocolError('Response must be an object')
                if (type(response.get('protocolVersion')) is not int or
                        response['protocolVersion'] != 1 or
                        response.get('requestId') != request_id):
                    raise ProtocolError('Response version or request ID mismatch')
                if response.get('success') is False:
                    error = response.get('error')
                    if not isinstance(error, dict) or not all(
                            isinstance(error.get(k), str) for k in ('code', 'message')):
                        raise ProtocolError('Malformed error response')
                    raise RemoteError(error)
                if response.get('success') is not True or not isinstance(response.get('result'), dict):
                    raise ProtocolError('Malformed success response')
                return response['result']
            try:
                return await asyncio.wait_for(exchange(), self.timeout)
            except RemoteError:
                raise
            except BaseException:
                await self.close()
                raise


GO = 'io.github.muzudho.kifuwarabego2026.games.go'


def document(content, suffix='sgf', media_type='application/x-go-sgf'):
    return dict(mediaType=media_type, schemaId=f'{GO}.{suffix}.v1', content=content)


def launch(room_type):
    return dict(version=1, requestId=uuid.uuid4().hex, roomTypeId=room_type,
                gameId=GO, playSpaceTypeId={'value': GO},
                configuration=document('{"boardSize":9}', 'configuration', 'application/json'),
                initialPosition=document('(;GM[1]SZ[9];B[aa];W[bb])'), participants=[])


async def open_room(client, room_type):
    request = launch(room_type)
    ready = await client.request('open', request)
    if (ready.get('requestId') != request['requestId'] or
            ready.get('roomTypeId') != room_type or
            not isinstance(ready.get('sessionId'), str) or not ready['sessionId']):
        await client.close()
        raise ProtocolError('Ready does not match the launch request')
    return ready['sessionId']


def require(condition, message):
    if not condition:
        raise ProtocolError(message)


async def scenario(command, room_type, trace=False, discard=False):
    async with Client(command, trace=trace) as client:
        session = await open_room(client, room_type)
        parameters = dict(sessionId=session)
        position = document('(;GM[1]SZ[9]AB[aa])')
        if room_type == 'board-editor':
            await client.request('replacePosition', dict(**parameters, position=position))
            result = await client.request('discard' if discard else 'adopt', parameters)
            require(result.get('status') == (1 if discard else 0), 'Incorrect editor status')
            require(result.get('position') == (None if discard else position), 'Incorrect returned position')
        elif room_type == 'review':
            view = await client.request('navigate', dict(**parameters, moveIndex=2))
            require(view.get('moveIndex') == 2, 'Incorrect review index')
            result = await client.request('usePosition', dict(**parameters, moveIndex=2, position=position))
            require(result.get('status') == 0 and result.get('position') == position and
                    result.get('moveIndex') == 2, 'Incorrect review selection')
        else:
            state = document('{"boardSize":9,"turn":"black"}', 'state', 'application/json')
            view = await client.request('updateState', dict(**parameters, revision=0, state=state))
            require(view.get('revision') == 0 and view.get('state') == state, 'Incorrect match state')
            for kind in range(3):
                action = dict(**parameters, actionId=f'action-{kind}', playerRoleId='black', kind=kind)
                if kind == 0:
                    action.update(x=2, y=3)
                accepted = await client.request('submitAction', action)
                require(accepted.get('actionId') == action['actionId'], 'Incorrect action ID')
            result = await client.request('complete', dict(**parameters, finalState=state,
                                                          winnerRoleId='white', reason='resignation'))
            require(result.get('status') == 0 and result.get('finalState') == state and
                    result.get('winnerRoleId') == 'white', 'Incorrect match completion')
        require(result.get('sessionId') == session, 'Completion session mismatch')
        await client.finish()
        return result


async def main():
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--room', choices=['board-editor', 'review', 'match'], required=True)
    parser.add_argument('--trace', action='store_true')
    parser.add_argument('--discard', action='store_true')
    parser.add_argument('command', nargs=argparse.REMAINDER)
    args = parser.parse_args()
    command = args.command[1:] if args.command[:1] == ['--'] else args.command
    if not command:
        parser.error('Supply -- <host executable> [arguments]')
    try:
        result = await scenario(command, args.room, args.trace, args.discard)
        print(json.dumps(result, ensure_ascii=False))
        return 0
    except (ProtocolError, OSError, ValueError, asyncio.TimeoutError) as error:
        print(f'FAIL: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(asyncio.run(main()))
