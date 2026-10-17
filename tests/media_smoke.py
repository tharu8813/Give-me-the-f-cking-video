"""Offline yt-dlp/FFmpeg integration using generated media and a loopback server.
Usage: python tests/media_smoke.py --tools-dir <directory containing yt-dlp.exe and ffmpeg.exe>
"""
import argparse
import functools
import http.server
import json
from pathlib import Path
import subprocess
import tempfile
import threading


def run(command):
    result = subprocess.run([str(x) for x in command], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=60)
    assert result.returncode == 0, result.stderr[-6000:]
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--tools-dir", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent
    harness = root / "GMTFV.Tests/bin/Debug/GMTFV.Tests.exe"
    ffmpeg = args.tools_dir / "ffmpeg.exe"
    yt = args.tools_dir / "yt-dlp.exe"
    assert harness.exists() and ffmpeg.exists() and yt.exists(), "Build the test harness and prepare the tools first"
    output = root / "GMTFV.Tests/bin/Debug"
    with tempfile.TemporaryDirectory(prefix="media-smoke-", dir=output) as folder:
        work = Path(folder)
        for name, source, codec in [("video.mp4", "testsrc2=size=160x120:rate=30:duration=1", "libx264"),
                                    ("video.webm", "testsrc2=size=160x120:rate=30:duration=1", "libvpx-vp9"),
                                    ("audio.m4a", "sine=frequency=440:duration=1", "aac"),
                                    ("audio.webm", "sine=frequency=440:duration=1", "libopus")]:
            run([ffmpeg, "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", source,
                 "-c:v" if name.startswith("video") else "-c:a", codec, work / name])

        class Handler(http.server.SimpleHTTPRequestHandler):
            def log_message(self, *_):
                pass

        server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), functools.partial(Handler, directory=str(work)))
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            base = f"http://127.0.0.1:{server.server_port}/"
            formats = []
            for name, video, codec in [("video.mp4", True, "avc1"), ("video.webm", True, "vp9"), ("audio.m4a", False, "aac"), ("audio.webm", False, "opus")]:
                formats.append(dict(format_id=name, url=base + name, ext=name.split(".")[-1], protocol="http",
                                    vcodec=codec if video else "none", acodec="none" if video else codec,
                                    height=120 if video else None, width=160 if video else None, fps=30 if video else None,
                                    tbr=100 if video else 64))
            fixture = work / "info.json"
            fixture.write_text(json.dumps(dict(id="BaW_jenozKc", title="GMTFV generated offline test", duration=1,
                                               extractor="generic", extractor_key="Generic", webpage_url=base,
                                               formats=formats)), encoding="utf-8")
            passed = 0
            for media_type, extensions in [("video", ["mp4", "mkv", "avi", "mov", "webm", "flv"]),
                                           ("audio", ["mp3", "m4a", "aac", "opus", "ogg", "wav", "flac"])]:
                for extension in extensions:
                    template = str(work / f"{media_type}-{extension}.%(ext)s")
                    command = json.loads(run([harness, "--emit-command", args.tools_dir, extension, media_type, template]).stdout)
                    # Supply our own generated metadata instead of contacting YouTube.
                    assert command[-2] == "--"
                    command = command[:-2] + ["--verbose", "--load-info-json", str(fixture)]
                    result = run([yt, *command])
                    lines = [line for line in result.stdout.splitlines() if line.startswith("GMTFV_FILE:")]
                    assert len(lines) == 1, result.stdout + result.stderr
                    path = Path(json.loads(lines[0].split(":", 1)[1]))
                    assert path.suffix == "." + extension and path.stat().st_size > 0, path
                    decoded = run([ffmpeg, "-hide_banner", "-i", path, "-f", "null", "-"])
                    if media_type == "video" and extension in ("mp4", "mkv", "mov"):
                        original = work / ("video.webm" if "Video: vp9" in decoded.stderr else "video.mp4")
                        def frames(media):
                            data = run([ffmpeg, "-v", "error", "-i", media, "-map", "0:v", "-f", "framemd5", "-"]).stdout
                            return [line.rsplit(",", 1)[-1].strip() for line in data.splitlines() if line and not line.startswith("#")]
                        expected, actual = frames(original), frames(path)
                        assert expected == actual, "Unexpected video re-encode"
                    passed += 1
                    print(f"PASS real yt-dlp/FFmpeg {media_type} {extension}", flush=True)
            print(f"RESULT: {passed} media integrations passed")
        finally:
            server.shutdown()
            server.server_close()
            thread.join()


if __name__ == "__main__":
    main()
