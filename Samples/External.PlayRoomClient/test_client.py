"""Run with python -m unittest discover -s Samples/External.PlayRoomClient."""
import asyncio
import json
import os
from pathlib import Path
import sys
import unittest

from client import Client, ProtocolError, RemoteError, launch, open_room, scenario


class TransportTests(unittest.IsolatedAsyncioTestCase):
    async def test_independent_board_editor(self):
        command = [sys.executable, str(Path(__file__).with_name('board_editor_host.py'))]
        await scenario(command, 'board-editor')
        await scenario(command, 'board-editor', discard=True)
        async with Client(command) as client:
            with self.assertRaises(RemoteError):
                await client.request('open', launch('board-editor'), version=999)
            session = await open_room(client, 'board-editor')
            with self.assertRaises(RemoteError):
                await client.request('adopt', {'sessionId': 'wrong'})
            await client.request('goodbye', {'sessionId': session})
            await client.finish()

    async def test_transport_failures_reap_child(self):
        fixtures = [
            "import time; time.sleep(30)",
            "print('not-json', flush=True)",
            "print('null', flush=True)",
            "print('x'*2000000, flush=True)",
            "import sys; sys.exit(23)",
            "import json; print(json.dumps(dict(protocolVersion=1, requestId='wrong', success=True, result={})), flush=True)",
        ]
        for fixture in fixtures:
            with self.subTest(fixture=fixture):
                async with Client([sys.executable, '-u', '-c', fixture], timeout=0.5) as client:
                    with self.assertRaises((ProtocolError, ValueError, OSError, asyncio.TimeoutError)):
                        await client.request('open', {})
                    self.assertIsNotNone(client.process.returncode)

    async def test_stderr_cannot_block_response(self):
        fixture = """import sys,json
r=json.loads(sys.stdin.readline())
sys.stderr.write('diagnostic'*100000); sys.stderr.flush()
print(json.dumps(dict(protocolVersion=1,requestId=r['requestId'],success=True,result={'ok':True})),flush=True)
"""
        async with Client([sys.executable, '-u', '-c', fixture]) as client:
            self.assertTrue((await client.request('open', {}))['ok'])
            await client.finish()

    async def test_completion_requires_exit(self):
        fixture = """import sys,json,time
r=json.loads(sys.stdin.readline())
print(json.dumps(dict(protocolVersion=1,requestId=r['requestId'],success=True,result={})),flush=True)
time.sleep(30)
"""
        async with Client([sys.executable, '-u', '-c', fixture], timeout=0.5) as client:
            await client.request('goodbye', {})
            with self.assertRaises(asyncio.TimeoutError):
                await client.finish()
            self.assertIsNotNone(client.process.returncode)


@unittest.skipUnless(os.environ.get('PLAYROOM_HOST_DIRECTORY'),
                     'Set PLAYROOM_HOST_DIRECTORY to a flat directory of built/published hosts')
class OfficialHostTests(unittest.IsolatedAsyncioTestCase):
    def command(self, kind):
        name = f'KifuwarabeGo2026.Reference.PlayRoomGui.{kind}.JsonLinesHost.dll'
        path = Path(os.environ['PLAYROOM_HOST_DIRECTORY']) / name
        self.assertTrue(path.is_file(), str(path))
        return ['dotnet', str(path)]

    async def test_three_rooms(self):
        for kind, room in [('BoardEditor', 'board-editor'), ('Review', 'review'), ('Match', 'match')]:
            with self.subTest(room=room):
                await scenario(self.command(kind), room)
        await scenario(self.command('BoardEditor'), 'board-editor', discard=True)

    async def test_rejections_leave_session_usable(self):
        for kind, room in [('BoardEditor', 'board-editor'), ('Review', 'review'), ('Match', 'match')]:
            async with Client(self.command(kind)) as client:
                with self.assertRaises(RemoteError):
                    await client.request('open', launch(room), version=999)
                session = await open_room(client, room)
                with self.assertRaises(RemoteError):
                    await client.request('goodbye', {'sessionId': 'incorrect'})
                await client.request('goodbye', {'sessionId': session})
                await client.finish()

    async def test_early_exit(self):
        for kind, room in [('BoardEditor', 'board-editor'), ('Review', 'review'), ('Match', 'match')]:
            async with Client(self.command(kind) + ['--exit-after-open']) as client:
                session = await open_room(client, room)
                with self.assertRaises((ProtocolError, OSError)):
                    await client.request('goodbye', {'sessionId': session})
                self.assertIsNotNone(client.process.returncode)


if __name__ == '__main__':
    unittest.main()
