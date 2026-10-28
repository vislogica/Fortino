// Capitolo 13, caso 13.2: la nota riservata del cliente nella pagina dell'ordine.
// Il pulsante chiede la nota al server (GET /orders/{id}/note) e mostra la risposta nella pagina:
// la nota se il server la concede, un messaggio di accesso negato se risponde 403.
window.fortinoCh13 = {
    async showNote(orderId) {
        const box = document.getElementById('nota-riservata');
        const response = await fetch(`/orders/${orderId}/note`);
        if (response.ok) {
            const data = await response.json();
            box.className = 'fortino-note-result is-shown';
            box.textContent = data.note;
        } else if (response.status === 403) {
            box.className = 'fortino-note-result is-denied';
            box.textContent = 'Accesso negato: la nota riservata la può leggere solo il cliente che ha fatto l\'ordine.';
        } else {
            box.className = 'fortino-note-result is-denied';
            box.textContent = `Errore ${response.status}: nota non disponibile.`;
        }
        box.hidden = false;
    },
};
