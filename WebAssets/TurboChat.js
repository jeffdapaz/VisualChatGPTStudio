const highlightExt = markedHighlight.markedHighlight({
    langPrefix: 'hljs language-',
    highlight(code, lang, info) {
        const language = hljs.getLanguage(lang) ? lang : 'plaintext';
        return hljs.highlight(code, { language }).value;
    }
});

function rawCode(el){
    return el.getAttribute('data-raw') || el.textContent;
}

function buildCodeBlock(lang, highlightedHtml, raw) {
    const id = 'cb-' + Math.random().toString(36).slice(2);
    return `
      <div class="code-box">
        <header>
          <span>${lang}</span>
          <div>
            <button onclick="sendCode('${id}','apply')">Apply</button>
            <button onclick="sendCode('${id}','copy')">Copy</button>
          </div>
        </header>
        <pre><code id="${id}" class="language-${lang}" data-raw="${raw.replace(/"/g, '&quot;')}">${highlightedHtml}</code></pre>
      </div>`;
}

function buildMermaidBlock(lang, text)
{
    const id = 'mmd-' + Math.random().toString(36).slice(2);
    return `
      <div class="mmd-box">
        <header>
          <span>${lang}</span>
          <div>
            <button onclick="sendCode('${id}','copy')">Copy</button>
          </div>
        </header>
        <div class="mermaid-box" id="${id}" data-raw="${text.replace(/"/g,'&quot;')}">
      </div>`
}

/* send messages to WebView2 */
function sendCode(id, action){
    const raw = document.getElementById(id).getAttribute('data-raw');
    window.chrome?.webview?.postMessage({
        action:action,
        code:raw
    });
}

const renderer = new marked.Renderer();

renderer.code = function ({ text, lang }) {
    if (lang?.toLowerCase() === 'mermaid') {
        return buildMermaidBlock(lang, text);
    }

    const highlighted = lang && hljs.getLanguage(lang)
        ? hljs.highlight(text, { language: lang }).value
        : hljs.highlightAuto(text).value;

    return buildCodeBlock(lang, highlighted, text);
};

marked.use({ renderer });

mermaid.initialize({ startOnLoad: false, suppressErrorRendering: true, theme: 'dark' });

/* --------- think + markdown --------- */
function splitThink(text){
    const parts = [];
    const thinkRegex = /^<think>([\s\S]*?)(<\/think>|$)/gi;
    let lastIdx = 0;
    let m;
    while((m = thinkRegex.exec(text)) !== null){
        if(m.index > lastIdx){
            parts.push({type:'md', text:text.slice(lastIdx, m.index)});
        }
        parts.push({type:'think', text:m[1]});
        lastIdx = thinkRegex.lastIndex;
    }
    if(lastIdx < text.length) parts.push({type:'md', text:text.slice(lastIdx)});
    return parts;
}

function highlightSpecialTags(text) {
    return text.replace(
        /(?<=^|[\s,>])([/@][^\s,\r\n]*)/g,
        (match) => {
            if (match.startsWith('/')) {
                return `<span class="command">${match}</span>`;
            }
            if (match.startsWith('@')) {
                return `<span class="context">${match}</span>`;
            }
            return match;
        }
    );
}

function renderFrag(f){
    if(f.type === 'think'){
        return `<details><summary>thinking…</summary>
              ${f.text.replace(/</g,'&lt;').replace(/>/g,'&gt;')}
          </details>`;
    }
    return marked.parse(f.text);
}

/* ---------- Functions called from C# ---------- */
function addMsg(role, rawText){
    updateShouldScrollFlag();
    const wrap = document.createElement('div');
    wrap.className = 'msg ' + role;
    const bubble = document.createElement('div');
    bubble.className = 'bubble';
    bubble.innerHTML = highlightSpecialTags(
        splitThink(rawText).map(renderFrag).join('')
    );
    wrap.appendChild(bubble);
    document.getElementById('chat').appendChild(wrap);
    scrollToBottomIfNeeded();
}

function updateLastGpt(rawText) {
    updateShouldScrollFlag();
    const last = document.querySelector('#chat .gpt:last-child .bubble');
    if (last) {
        last.innerHTML = splitThink(rawText).map(renderFrag).join('');
    } else {
        addMsg('gpt', rawText);
    }
    scrollToBottomIfNeeded();
}

function clearChat(){
    document.getElementById('chat').innerHTML = '';
}

function renderMermaid() {
    updateShouldScrollFlag();
    document
        .querySelectorAll('.mermaid-box:not(.processed)')
        .forEach(box => {
            box.classList.add('processed');
            const def = box.getAttribute('data-raw');
            mermaid.render('svg-' + box.id, def)
                .then(({svg}) => {
                    box.innerHTML = svg;
                    scrollToBottomIfNeeded();
                })
                .catch(err => {
                    box.innerHTML = '<pre style="color:red">mermaid: ' + err + '</pre>';
                    scrollToBottomIfNeeded();
                })
        });
}

/* ---------- Scroll ---------- */
let shouldAutoScroll = true;

const chat = document.scrollingElement;

function isScrolledToBottom() {
    return chat.scrollHeight - chat.scrollTop - chat.clientHeight <= 10;
}

function updateShouldScrollFlag() {
    shouldAutoScroll = isScrolledToBottom();
}

function scrollToBottomIfNeeded() {
    if (shouldAutoScroll) {
        window.scrollTo(0, chat.scrollHeight);
    }
}

chat.addEventListener('scroll', updateShouldScrollFlag);
window.addEventListener('resize', updateShouldScrollFlag);