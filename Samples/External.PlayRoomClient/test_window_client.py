import asyncio
import os
import unittest
from client import Client, RemoteError
from window_client import request, state, run


@unittest.skipUnless(os.environ.get('GO_WINDOW_HOST'), 'Set GO_WINDOW_HOST to the built Go Windows host DLL')
class WindowProtocolTests(unittest.IsolatedAsyncioTestCase):
    def command(self):
        return ['dotnet', os.environ['GO_WINDOW_HOST'], '--stdio-contract-smoke']

    async def test_state_and_completion(self):
        await run(self.command(), smoke=True)

    async def test_invalid_updates_do_not_consume_revision(self):
        async with Client(self.command(), timeout=35) as client:
            ready = await client.request('open', request())
            session = ready['sessionId']
            async def update(revision, value):
                return await client.request('updateState', dict(sessionId=session, revision=revision, state=value))
            await update(0, state())
            with self.assertRaises(RemoteError):
                await update(0, state())
            with self.assertRaises(RemoteError):
                await update(1, state([dict(x=99, y=0, color='black')]))
            await update(1, state([dict(x=0, y=0, color='white')]))
            with self.assertRaises(RemoteError):
                await client.request('readEvents', dict(sessionId='wrong'))
            with self.assertRaises(RemoteError):
                await client.request('submitAction', dict(sessionId=session))
            await client.request('goodbye', dict(sessionId=session))
            await client.finish()

    async def test_eof_closes_host(self):
        async with Client(self.command(), timeout=35) as client:
            await client.request('open', request())
            client.process.stdin.close()
            await client.finish()

    async def test_rejected_open_can_be_corrected(self):
        async with Client(self.command(), timeout=35) as client:
            invalid = request()
            invalid['roomTypeId'] = 'review'
            with self.assertRaises(RemoteError):
                await client.request('open', invalid)
            ready = await client.request('open', request())
            await client.request('goodbye', dict(sessionId=ready['sessionId']))
            await client.finish()
