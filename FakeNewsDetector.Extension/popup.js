const API_URL = 'http://localhost:5011/api/news/analyze';

const form = document.getElementById('analysis-form');
const button = document.getElementById('analyze-button');
const status = document.getElementById('status');
const result = document.getElementById('result');

document.addEventListener('DOMContentLoaded', async () => {
  try {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tab?.id) return;

    let page;
    try {
      page = await chrome.tabs.sendMessage(tab.id, { type: 'readArticle' });
    } catch {
      const [injected] = await chrome.scripting.executeScript({
        target: { tabId: tab.id },
        func: () => {
          const article = document.querySelector('article');
          const main = document.querySelector('main');
          const contentElement = article || main || document.body;
          const titleElement = document.querySelector('h1') || document.querySelector('title');
          return {
            title: titleElement?.innerText?.trim() || document.title,
            content: contentElement.innerText.trim().slice(0, 50000),
            source: window.location.hostname
          };
        }
      });
      page = injected.result;
    }

    if (page?.title) document.getElementById('title').value = page.title;
    if (page?.content) document.getElementById('content').value = page.content;
    if (page?.source) document.getElementById('source').value = page.source;
  } catch {
    status.textContent = 'Enter the article details manually on this page.';
  }
});

form.addEventListener('submit', async (event) => {
  event.preventDefault();
  button.disabled = true;
  status.textContent = 'Analyzing...';
  result.classList.add('hidden');

  const payload = {
    title: document.getElementById('title').value.trim(),
    content: document.getElementById('content').value.trim(),
    source: document.getElementById('source').value.trim() || null
  };

  try {
    const response = await fetch(API_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      const details = await response.text();
      throw new Error(details || `Request failed (${response.status})`);
    }
    const analysis = await response.json();

    const verdictNames = ['fake', 'real', 'uncertain'];
    const verdict = typeof analysis.verdict === 'number'
      ? (verdictNames[analysis.verdict] || 'uncertain')
      : String(analysis.verdict).toLowerCase();
    const verdictData = {
      real: { emoji: '✅', subtitle: 'This article appears credible.' },
      fake: { emoji: '🚨', subtitle: 'This article shows signs of misinformation.' },
      uncertain: { emoji: '⚠️', subtitle: 'More verification is recommended.' }
    }[verdict] || { emoji: '🔎', subtitle: 'Analysis completed.' };
    const percentage = Math.round(analysis.confidenceScore * 100);
    result.className = `result ${verdict}`;
    document.getElementById('verdict-emoji').textContent = verdictData.emoji;
    document.getElementById('verdict').textContent = verdict;
    document.getElementById('verdict-subtitle').textContent = verdictData.subtitle;
    document.getElementById('confidence').textContent = `${percentage}%`;
    document.getElementById('confidence-bar').style.width = `${percentage}%`;
    document.getElementById('explanation').textContent = analysis.explanation;
    status.textContent = '';
    result.classList.remove('hidden');
  } catch (error) {
    const message = String(error?.message || error).toLowerCase();
    if (message.includes('failed to fetch') || message.includes('networkerror') || message.includes('network error') || message.includes('offline')) {
      status.textContent = 'No internet connection. Please connect to the internet and try again.';
    } else if (message.includes('localhost') || message.includes('connection refused')) {
      status.textContent = 'The analysis service is unavailable. Please start the backend and try again.';
    } else {
      status.textContent = 'Unable to analyze this article right now. Please try again.';
    }
  } finally {
    button.disabled = false;
  }
});
