const endpoint = 'http://127.0.0.1:43128';
const isYouTubeUrl = url => {
  try {
    const host = new URL(url).hostname.toLowerCase();
    return host === 'youtube.com' || host.endsWith('.youtube.com') || host === 'youtu.be';
  } catch { return false; }
};

let lastCompletedRequest = '';
let polling = false;

async function getTabs(mode) {
  if (mode === 'current') return chrome.tabs.query({ active: true, currentWindow: true });
  if (mode === 'selected') return chrome.tabs.query({ highlighted: true, currentWindow: true });
  if (mode === 'all') return chrome.tabs.query({ currentWindow: true });
  return [];
}

async function pollRequest() {
  if (polling) return;
  polling = true;
  try {
    const response = await fetch(`${endpoint}/request`, { cache: 'no-store' });
    if (!response.ok) return;
    const request = await response.json();
    if (!request.requestId || request.requestId === lastCompletedRequest) return;
    const tabs = await getTabs(request.mode);
    const urls = [...new Set(tabs.map(tab => tab.url).filter(isYouTubeUrl))];
    const sent = await fetch(`${endpoint}/tabs`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ requestId: request.requestId, urls })
    });
    if (sent.ok) lastCompletedRequest = request.requestId;
  } catch {
    // GMTFV가 실행 중이 아닐 때는 조용히 다음 폴링을 기다립니다.
  } finally {
    polling = false;
  }
}

setInterval(pollRequest, 1500);
chrome.runtime.onInstalled.addListener(() => {
  chrome.alarms.create('gmtfv-poll', { periodInMinutes: 0.5 });
  pollRequest();
});
chrome.runtime.onStartup.addListener(pollRequest);
chrome.alarms.onAlarm.addListener(alarm => { if (alarm.name === 'gmtfv-poll') pollRequest(); });
pollRequest();
