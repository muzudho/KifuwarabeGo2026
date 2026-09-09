"""External caller for the Go window's polling capability. No GUI/DLL imports."""
import argparse
import asyncio
import json
import sys
from client import Client, document, launch, ProtocolError, require


def request():
    value = launch('match')
    value['initialPosition'] = None
    value['configuration']['content'] = json.dumps(dict(version=1, boardSize=9, komi=6.5,
        ruleset='chinese-area', startingPlayer='black', setupStones=[]))
    return value


def state(stones=None, turn='black'):
    return document(json.dumps(dict(version=1, boardSize=9, currentTurn=turn, stones=stones or [])),
                    'display-state', 'application/json')


async def run(command, smoke=False):
    async with Client(command, timeout=35, trace=True) as client:
        description = await client.request('describe', {})
        require('poll-input-events.v1' in description['capabilities'], 'Polling not supported')
        launch_request = request()
        ready = await client.request('open', launch_request)
        require(ready['requestId'] == launch_request['requestId'] and ready['roomTypeId'] == 'match', 'Wrong ready')
        session = ready['sessionId']
        revision = 0
        displayed = state()
        await client.request('updateState', dict(sessionId=session, revision=revision, state=displayed))
        if smoke:
            # Exercise the production transport/state projection; no synthetic GUI input.
            displayed = state([dict(x=2, y=3, color='black')], 'white')
            response = await client.request('updateState', dict(sessionId=session, revision=1, state=displayed))
            require(response['state'] == displayed, 'State not returned intact')
            events = await client.request('readEvents', dict(sessionId=session))
            require(events['events'] == [] and not events['closed'], 'Unexpected input')
            await client.request('complete', dict(sessionId=session, finalState=displayed,
                                                 winnerRoleId=None, reason='contract-smoke'))
            await client.finish()
            return
        print('Input is logged and rejected by this transport demo; it does not implement Go rules.', file=sys.stderr)
        while True:
            events = await client.request('readEvents', dict(sessionId=session))
            for event in events['events']:
                if event['type'] == 'action':
                    print('INPUT ' + json.dumps(event), file=sys.stderr)
                    # Return the same board with a newer projection revision to release input.
                    revision += 1
                    if not events['closed']:
                        await client.request('updateState', dict(sessionId=session, revision=revision, state=displayed))
            if events['closed']:
                await client.finish()
                return
            await asyncio.sleep(0.1)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--smoke', action='store_true')
    parser.add_argument('command', nargs=argparse.REMAINDER)
    args = parser.parse_args()
    command = args.command[1:] if args.command[:1] == ['--'] else args.command
    if not command:
        parser.error('Supply -- <host> --stdio or --stdio-contract-smoke')
    try:
        asyncio.run(run(command, args.smoke))
    except (ProtocolError, OSError, ValueError, asyncio.TimeoutError) as error:
        print(f'FAIL: {error}', file=sys.stderr)
        sys.exit(1)
