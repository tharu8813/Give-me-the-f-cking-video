const tabsBox = document.getElementById('tabs');
const status = document.getElementById('status');
const buttons = [...document.querySelectorAll('button')];
let youtubeTabs = [];
const isYouTubeUrl = url => { try { const host = new URL(url).hostname.toLowerCase(); return host === 'youtube.com' || host.endsWith('.youtube.com') || host === 'youtu.be'; } catch { return false; } };
const escapeHtml = value => value.replace(/[&<>"]/g, character => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[character]));

async function send(urls) {
  if (!urls.length) { status.textContent = '보낼 YouTube 탭이 없습니다.'; return; }
  buttons.forEach(button => button.disabled = true); status.textContent = 'GMTFV로 보내는 중…';
  try {
    const response = await fetch('http://127.0.0.1:43128/tabs', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify([...new Set(urls)]) });
    if (!response.ok) throw new Error();
    status.textContent = `${urls.length}개 탭을 GMTFV로 보냈습니다.`;
  } catch { status.textContent = 'GMTFV에 연결하지 못했습니다. 프로그램이 실행 중인지 확인해주세요.'; buttons.forEach(button => button.disabled = false); }
}

(async () => {
  const tabs = await chrome.tabs.query({ currentWindow: true });
  youtubeTabs = tabs.filter(tab => isYouTubeUrl(tab.url));
  tabsBox.innerHTML = youtubeTabs.length ? youtubeTabs.map((tab, index) => `<label class="tab"><input type="checkbox" data-index="${index}" checked><span>${escapeHtml(tab.title || tab.url)}</span></label>`).join('') : '<div class="hint">현재 창에 열린 YouTube 탭이 없습니다.</div>';
  document.getElementById('current').onclick = async () => { const [tab] = await chrome.tabs.query({ active: true, currentWindow: true }); await send(isYouTubeUrl(tab?.url) ? [tab.url] : []); };
  document.getElementById('selected').onclick = () => send([...tabsBox.querySelectorAll('input:checked')].map(input => youtubeTabs[Number(input.dataset.index)].url));
  document.getElementById('all').onclick = () => send(youtubeTabs.map(tab => tab.url));
})();
