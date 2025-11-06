document.addEventListener('DOMContentLoaded', function () {
    const btn = document.getElementById('chatbot-button');
    const panel = document.getElementById('chatbot-panel');
    const closeBtn = document.getElementById('chatbot-close');
    const sendBtn = document.getElementById('chatbot-send');
    const input = document.getElementById('chatbot-input');
    const messages = document.getElementById('chatbot-messages');
    const status = document.getElementById('chatbot-status');

    function appendMessage(who, text) {
        const el = document.createElement('div');
        el.style.marginBottom = '8px';
        el.innerHTML = `<div style="font-size:12px;color:#9aa0b4">${who}</div><div style="padding:8px;border-radius:8px;background:${who==='Tú' ? '#2c5be6' : 'rgba(255,255,255,0.03)'};color:${who==='Tú' ? '#fff' : '#ddd'}">${text}</div>`;
        messages.appendChild(el);
        messages.scrollTop = messages.scrollHeight;
    }

    btn.addEventListener('click', () => {
        panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
        input.focus();
    });

    closeBtn.addEventListener('click', () => { panel.style.display = 'none'; });

    async function sendMessage() {
        const text = input.value && input.value.trim();
        if (!text) return;
        appendMessage('Tú', text);
        input.value = '';
        status.style.display = 'block';
        status.textContent = 'Enviando...';

        try {
            const res = await fetch('/api/chatbot/message', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: text })
            });

            if (!res.ok) {
                const err = await res.json().catch(()=>({error:'Error'}));
                appendMessage('Asistente', 'Lo siento, hubo un error: ' + (err?.error || res.statusText));
            } else {
                const data = await res.json();
                if (data.isSuccess) appendMessage('Asistente', data.reply);
                else appendMessage('Asistente', 'Error: ' + (data.error || 'respuesta inválida'));
            }
        } catch (ex) {
            appendMessage('Asistente', 'Error de red: ' + ex.message);
        } finally {
            status.style.display = 'none';
        }
    }

    sendBtn.addEventListener('click', sendMessage);
    input.addEventListener('keydown', (e) => { if (e.key === 'Enter') sendMessage(); });
});
