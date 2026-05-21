import http.server
import os
import socketserver
import sys


class UnityWebGLHandler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {
        **http.server.SimpleHTTPRequestHandler.extensions_map,
        ".js": "application/javascript",
        ".wasm": "application/wasm",
        ".data": "application/octet-stream",
        ".symbols.json": "application/json",
    }

    def guess_type(self, path):
        if path.endswith(".br") or path.endswith(".gz"):
            return self.guess_type(path.rsplit(".", 1)[0])
        return super().guess_type(path)

    def end_headers(self):
        path = self.translate_path(self.path)
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
            self.send_header("Vary", "Accept-Encoding")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
            self.send_header("Vary", "Accept-Encoding")
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()


def main():
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8126
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    with socketserver.TCPServer(("127.0.0.1", port), UnityWebGLHandler) as httpd:
        print(f"Serving HTTP on 127.0.0.1 port {port} (http://127.0.0.1:{port}/) ...")
        httpd.serve_forever()


if __name__ == "__main__":
    main()
