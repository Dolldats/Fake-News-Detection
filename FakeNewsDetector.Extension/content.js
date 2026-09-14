chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type !== 'readArticle') return;

  const article = document.querySelector('article');
  const main = document.querySelector('main');
  const contentElement = article || main || document.body;
  const content = contentElement.innerText.trim();
  const titleElement = document.querySelector('h1') || document.querySelector('title');

  sendResponse({
    title: titleElement?.innerText?.trim() || document.title,
    content: content.slice(0, 50000),
    source: window.location.hostname
  });
});
