"""Offline yt-dlp/FFmpeg integration using generated media and a loopback server.
Usage: python tests/media_smoke.py --tools-dir <directory containing yt-dlp.exe and ffmpeg.exe>
"""
import argparse
import functools
import hashlib
import time
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
    harness = root / "GMTFV.Tests/bin/Debug/net10.0-windows/GMTFV.Tests.exe"
    ffmpeg = args.tools_dir / "ffmpeg.exe"
    yt = args.tools_dir / "yt-dlp.exe"
    assert harness.exists() and ffmpeg.exists() and yt.exists(), "Build the test harness and prepare the tools first"
    output = harness.parent
    with tempfile.TemporaryDirectory(prefix="한글 공백-media-smoke-", dir=output) as folder:
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
                                           ("audio", ["original", "mp3", "m4a", "aac", "opus", "ogg", "wav", "flac"])]:
                for extension in extensions:
                    target = work / f"{media_type}-{extension}.{extension}"
                    started = time.perf_counter()
                    result = run([harness, "--media-download", args.tools_dir, extension, media_type, fixture, target])
                    path = Path(json.loads(result.stdout))
                    seconds = time.perf_counter() - started
                    assert path.suffix == (".webm" if extension == "original" else "." + extension) and path.stat().st_size > 0, path
                    assert "STAGE Completed/" in result.stderr and "STAGE Validating/Save" in result.stderr
                    print(f"METRIC engine {media_type} {extension}: {seconds:.3f}s", flush=True)
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
            # Probe every explicit audio policy with real codecs, including same-extension mismatches.
            for name, codec in [("aac.aac", "aac"), ("mp3.mp3", "libmp3lame"), ("vorbis.ogg", "libvorbis"), ("opus.opus", "libopus"), ("flac.flac", "flac"), ("pcm.wav", "pcm_s24le")]:
                run([ffmpeg, "-v", "error", "-f", "lavfi", "-i", "sine=frequency=440:duration=1", "-c:a", codec, work / name])
            cases = [("audio.m4a", "m4a", "aac", "direct"), ("audio.m4a", "aac", "aac", "copy"),
                     ("aac.aac", "m4a", "aac", "copy"), ("audio.webm", "opus", "opus", "copy"),
                     ("opus.opus", "opus", "opus", "direct"), ("vorbis.ogg", "ogg", "vorbis", "direct"),
                     ("opus.opus", "ogg", "vorbis", "encode"), ("mp3.mp3", "mp3", "mp3", "direct"),
                     ("flac.flac", "flac", "flac", "direct"), ("pcm.wav", "wav", "pcm_s24le", "direct"),
                     ("audio.m4a", "flac", "flac", "encode"), ("audio.m4a", "wav", "pcm_s16le", "encode"),
                     ("audio.m4a", "original", "aac", "direct"), ("audio.webm", "original", "opus", "direct")]
            for index, (source_name, extension, codec, policy) in enumerate(cases):
                source = work / source_name
                info = dict(id="BaW_jenozKc", title="Audio policy integration", duration=1, extractor="generic", extractor_key="Generic", webpage_url=base,
                            formats=[dict(format_id="source", url=base + source_name, ext=source.suffix[1:], protocol="http", vcodec="none", acodec=codec)])
                fixture.write_text(json.dumps(info), encoding="utf-8")
                target = work / f"policy-{index}.{extension}"
                result = run([harness, "--media-download", args.tools_dir, extension, "audio", fixture, target])
                path = Path(json.loads(result.stdout))
                probe = json.loads(run([args.tools_dir / "ffprobe.exe", "-v", "error", "-show_streams", "-show_format", "-of", "json", path]).stdout)
                assert probe["streams"][0]["codec_name"] == codec, probe
                if policy == "direct":
                    assert hashlib.sha256(source.read_bytes()).digest() == hashlib.sha256(path.read_bytes()).digest(), "Direct save changed media bytes"
                    assert "Converting/EncodeAudio" not in result.stderr and "Converting/Remux" not in result.stderr
                elif policy == "copy":
                    assert "Converting/Remux" in result.stderr and "Converting/EncodeAudio" not in result.stderr
                else:
                    assert "Converting/EncodeAudio" in result.stderr
                run([ffmpeg, "-v", "error", "-i", path, "-f", "null", "-"])
                passed += 1
                print(f"PASS real policy {source_name} -> {extension}: {policy}", flush=True)
            # Cancel the owned FFmpeg while it processes real input, then recover from a real encoder failure.
            fixture.write_text(json.dumps(dict(id="BaW_jenozKc", title="Cancel audio fixture", duration=1, extractor="generic", extractor_key="Generic", webpage_url=base,
                formats=[dict(format_id="source", url=base + "audio.webm", ext="webm", vcodec="none", acodec="opus", protocol="http")])), encoding="utf-8")
            cancelled = run([harness, "--media-download", args.tools_dir, "mp3", "audio", fixture, work / "cancelled.mp3", "cancel-audio"])
            assert json.loads(cancelled.stdout) == "cancelled" and "STAGE Cancelled/" in cancelled.stderr and "STAGE Completed/" not in cancelled.stderr
            passed += 1; print("PASS cancel actual FFmpeg audio conversion", flush=True)
            failed = subprocess.run([str(x) for x in [harness, "--media-download", args.tools_dir, "mp3", "audio", fixture, work / "recover.mp3", "fail-audio"]], capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=60)
            assert failed.returncode != 0 and "STAGE Failed/" in failed.stderr and "STAGE Completed/" not in failed.stderr
            recovered = run([harness, "--media-download", args.tools_dir, "mp3", "audio", fixture, work / "recover.mp3"])
            run([ffmpeg, "-v", "error", "-i", Path(json.loads(recovered.stdout)), "-f", "null", "-"])
            passed += 1; print("PASS actual FFmpeg failure and manual retry recovery", flush=True)
            fixture.write_text(json.dumps(dict(id="BaW_jenozKc", title="Cancel merge fixture", duration=1, extractor="generic", extractor_key="Generic", webpage_url=base, formats=formats)), encoding="utf-8")
            cancelled = run([harness, "--media-download", args.tools_dir, "mp4", "video", fixture, work / "cancelled-merge.mp4", "cancel-merge"])
            assert json.loads(cancelled.stdout) == "cancelled" and "STAGE Merging/Merge" in cancelled.stderr and "STAGE Completed/" not in cancelled.stderr
            passed += 1; print("PASS cancel yt-dlp FFmpeg merge", flush=True)
            # Combined stream fallback still extracts audio without re-encoding AAC.
            combined = work / "combined.mp4"
            run([ffmpeg, "-v", "error", "-i", work / "video.mp4", "-i", work / "audio.m4a", "-c", "copy", combined])
            fixture.write_text(json.dumps(dict(id="BaW_jenozKc", title="Combined fallback", duration=1, extractor="generic", extractor_key="Generic", webpage_url=base,
                formats=[dict(format_id="combined", url=base + "combined.mp4", ext="mp4", vcodec="h264", acodec="aac", protocol="http", height=120, fps=30)])), encoding="utf-8")
            for kind, extension in [("video", "mp4"), ("video", "mkv"), ("audio", "m4a")]:
                result = run([harness, "--media-download", args.tools_dir, extension, kind, fixture, work / f"combined-{kind}.{extension}"])
                path = Path(json.loads(result.stdout)); run([ffmpeg, "-v", "error", "-i", path, "-f", "null", "-"])
                if kind == "audio":
                    assert "Converting/Remux" in result.stderr
                passed += 1
                print(f"PASS combined source {kind} {extension}", flush=True)
            print(f"RESULT: {passed} media integrations passed")
        finally:
            server.shutdown()
            server.server_close()
            thread.join()


if __name__ == "__main__":
    main()
