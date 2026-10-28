// Capitolo 17: la chat dell'assistente del portale clienti.
// Le domande sugli ordini di qualcuno ("mostrami gli ordini...") vanno allo strumento che legge gli
// ordini (/assistant/orders, caso 17.2); tutte le altre alla risposta libera (/assistant/ask, caso 17.1).
window.fortinoCh17 = {
    async ask() {
        const input = document.getElementById('question');
        const question = input.value.trim();
        if (!question) return;
        input.value = '';
        this.add(question, 'is-user');
        const wantsOrders = /\bordini\b/i.test(question) && /\b(mostra|elenca|quali|vedere)/i.test(question);
        const response = await fetch(wantsOrders ? '/assistant/orders' : '/assistant/ask', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ question }),
        });
        const data = response.ok ? await response.json() : { answer: `Errore ${response.status}.` };
        this.add(data.answer, 'is-assistant');
    },
    add(text, kind) {
        const chat = document.getElementById('chat');
        const bubble = document.createElement('div');
        bubble.className = `fortino-chat-bubble ${kind}`;
        bubble.textContent = text;
        chat.appendChild(bubble);
        chat.scrollTop = chat.scrollHeight;
    },
};
