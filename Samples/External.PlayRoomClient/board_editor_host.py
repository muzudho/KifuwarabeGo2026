"""Independent headless Board Editor v1 reference host; no .NET dependencies."""
import json
import sys
import uuid

GO = 'io.github.muzudho.kifuwarabego2026.games.go'


def main():
    # The public wire format is UTF-8 regardless of the console code page.
    sys.stdin.reconfigure(encoding='utf-8')
    sys.stdout.reconfigure(encoding='utf-8')
    session = None
    position = None
    for line in sys.stdin:
        request_id = ''
        finished = False
        try:
            request = json.loads(line)
            if not isinstance(request, dict):
                raise ValueError('Request must be an object')
            request_id = request.get('requestId', '')
            if not isinstance(request_id, str) or not request_id.strip():
                request_id = ''
                raise ValueError('requestId is required')
            if type(request.get('protocolVersion')) is not int or request['protocolVersion'] != 1:
                raise ValueError('Unsupported protocolVersion')
            p = request.get('parameters')
            if not isinstance(p, dict):
                raise ValueError('parameters must be an object')
            method = request.get('method')
            if method == 'open':
                if session is not None:
                    raise ValueError('Already open')
                if (p.get('version') != 1 or p.get('roomTypeId') != 'board-editor' or
                        p.get('gameId') != GO or not isinstance(p.get('requestId'), str)):
                    raise ValueError('Unsupported launch request')
                position = read_document(p.get('initialPosition'))
                session = uuid.uuid4().hex
                result = dict(requestId=p['requestId'], sessionId=session, roomTypeId='board-editor')
            else:
                if session is None or p.get('sessionId') != session:
                    raise ValueError('sessionId mismatch')
                if method == 'replacePosition':
                    position = read_document(p.get('position'))
                    result = dict(requestId=request_id, sessionId=session, roomTypeId='board-editor')
                elif method in ('adopt', 'discard', 'goodbye'):
                    result = dict(sessionId=session, status={'adopt': 0, 'discard': 1, 'goodbye': 2}[method],
                                  position=position if method == 'adopt' else None)
                    finished = True
                else:
                    raise ValueError('Unknown method')
            response = dict(protocolVersion=1, requestId=request_id, success=True, result=result, error=None)
        except (ValueError, TypeError) as error:
            response = dict(protocolVersion=1, requestId=request_id, success=False, result=None,
                            error=dict(code='invalid-request', message=str(error)))
            print(str(error), file=sys.stderr, flush=True)
        print(json.dumps(response, ensure_ascii=False), flush=True)
        if finished or ('--exit-after-open' in sys.argv and session is not None):
            return


def read_document(value):
    if not isinstance(value, dict) or not all(isinstance(value.get(k), str)
                                            for k in ('mediaType', 'schemaId', 'content')):
        raise ValueError('A document requires mediaType, schemaId and string content')
    return value


if __name__ == '__main__':
    main()
