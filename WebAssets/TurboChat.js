const highlightExt = markedHighlight.markedHighlight({
    langPrefix: 'hljs language-',
    highlight(code, lang, info) {
        const language = hljs.getLanguage(lang) ? lang : 'plaintext';
        return hljs.highlight(code, { language }).value;
    }
});

function buildCodeBlock(lang, highlightedHtml, raw) {
    const id = 'cb-' + Math.random().toString(36).slice(2);
    return `
      <div class="code-box">
        <header>
          <span>${lang}</span>
          <div>
            <button class="tooltip" onclick="sendCode('${id}','apply')">
              <i class="fa-solid fa-terminal"></i>
              <span class="tooltiptext">Apply</span>
            </button>
            <button class="copy-button tooltip" onclick="sendCode('${id}','copy')">
              <i class="fa-regular fa-copy"></i>
              <span class="tooltiptext">Copy to clipboard</span>
            </button>
          </div>
        </header>
        <pre><code id="${id}" class="language-${lang}">${highlightedHtml}</code></pre>
      </div>`;
}

function buildMermaidBlock(lang, text)
{
    const id = 'mmd-' + Math.random().toString(36).slice(2);
    return `
      <div class="code-box">
        <header>
          <span>${lang}</span>
          <div>
            <button class="copy-button tooltip" onclick="sendPNG('${id}')">
              <i class="fa-regular fa-image"></i>
              <span class="tooltiptext">Copy as image</span>
            </button>
            <button class="copy-button tooltip" onclick="sendCode('${id}','copy')">
              <i class="fa-regular fa-copy"></i>
              <span class="tooltiptext">Copy as text</span>
            </button>
          </div>
        </header>
        <div class="mermaid-box" id="${id}" data-raw="${text.replace(/"/g,'&quot;')}"></div>
      </div>`
}

/* send messages to WebView2 */
function sendPNG(id) {
    const svgEl = document.querySelector('#'+id+' svg');
    const svgString = new XMLSerializer().serializeToString(svgEl);
    const svgBase64 = 'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgString)));
    const img = new Image();
    const minSize = 1024;
    const bgColor = getComputedStyle(svgEl).getPropertyValue('--code-bg-color').trim();
    img.onload = () => {
        const canvas = document.createElement('canvas');
        const scaleW = minSize / img.width;
        const scaleH = minSize / img.height;
        const scale = Math.max(scaleW, scaleH);
        canvas.width  = img.width  * scale;
        canvas.height = img.height * scale;
        const ctx = canvas.getContext('2d');
        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = 'high';
        if (bgColor) {
            ctx.fillStyle = bgColor;
            ctx.fillRect(0, 0, canvas.width, canvas.height);
        }
        ctx.drawImage(img, 0, 0);

        const base64 = canvas.toDataURL('image/png').split(',')[1];
        window.chrome.webview.postMessage({
            action : 'png',
            data : base64
        });
    };
    img.src = svgBase64;
}

function sendCode(id, action){
    let element = document.getElementById(id);
    let raw = element.getAttribute("data-raw") ?? element.textContent;
    let selection = window.getSelection();
    if (selection.rangeCount > 0 && id === selection.getRangeAt(0).commonAncestorContainer.id) {
        raw = selection.toString();
    }
    window.chrome?.webview?.postMessage({
        action: action,
        data: raw
    });
    if (action === 'copy') {
        let popup = document.getElementById("popup");
        if (!popup.classList.contains('show')) {
            popup.classList.add('show');
            setTimeout(() => {
                popup.classList.remove('show');
            }, 2000);
        }
    }
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
        /(?<=^|[\s,>])((\/{2}|@)[^\s,\r\n]+)/g,
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
function addMsg(role, rawText, scrollTo = true){
    const wrap = document.createElement('div');
    wrap.className = 'msg ' + role;
    const bubble = document.createElement('div');
    bubble.className = 'bubble';
    bubble.innerHTML = highlightSpecialTags(
        splitThink(rawText).map(renderFrag).join('')
    );
    wrap.appendChild(bubble);
    document.getElementById('chat').appendChild(wrap);
    if (role !== 'gpt') {
        scrollToLastRequest();
    }
}

function updateLastGpt(rawText, scrollTo = true) {
    updateShouldScrollFlag();
    const last = document.querySelector('#chat .gpt:last-child .bubble');
    if (last) {
        last.innerHTML = splitThink(rawText).map(renderFrag).join('');
    } else {
        addMsg('gpt', rawText, scrollTo);
    }
    if (scrollTo) {
        scrollToBottomIfNeeded();
    }
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

function scrollToLastResponse() {
    document.querySelector('#chat .gpt:last-child .bubble')?.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });
}

function scrollToLastRequest() {
    document.querySelector('#chat .user:last-child .bubble')?.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });
}

chat.addEventListener('scroll', updateShouldScrollFlag);
window.addEventListener('resize', updateShouldScrollFlag);