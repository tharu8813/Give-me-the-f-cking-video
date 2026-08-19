const status = document.getElementById('status');
const button = document.getElementById('import');
function isYouTubeUrl(url) {
  try { const host = new URL(url).hostname.toLowerCase(); return host === 'youtube.com' || host.endsWith('.youtube.com') || host === 'youtu.be'; } catch { return false; }
}
button.addEventListener('click', async () => {
  button.disabled = true; status.textContent = '열린 탭을 확인하는 중…';
  const tabs = await chrome.tabs.query({});
  const urls = [...new Set(tabs.map(tab => tab.url).filter(isYouTubeUrl))];
  if (urls.length === 0) { status.textContent = '열린 YouTube 탭이 없습니다.'; button.disabled = false; return; }
  try {
    const response = await fetch('http://127.0.0.1:43128/tabs', { method: 'POST', headers: { 'Content-Type': 'text/plain;charset=UTF-8' }, body: JSON.stringify(urls) });
    if (!response.ok) throw new Error();
    status.textContent = `${urls.length}개 탭을 GMTFV로 보냈습니다.`;
  } catch { status.textContent = 'GMTFV에 연결하지 못했습니다. 프로그램이 실행 중인지 확인해 주세요.'; button.disabled = false; }
});
