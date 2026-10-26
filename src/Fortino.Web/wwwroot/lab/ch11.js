// Capitolo 11, caso 11.2: il selettore della quantità del portale clienti.
// La pagina Blazor registra qui il suo oggetto QuantityEditor. Quando il cliente sposta un
// cursore, questo script chiama il metodo [JSInvokable] SetQuantity sull'oggetto .NET.
// L'oggetto resta raggiungibile anche dalla console del browser (window.fortinoCh11.editor):
// è proprio quello che il caso 11.2 dimostra.
window.fortinoCh11 = {
    editor: null,
    page: null,     // la pagina, solo per rileggere la tabella dopo ogni modifica
    register(editor, page) {
        this.editor = editor;
        this.page = page ?? null;
    },
};

function ch11Find(className, line) {
    return document.querySelector(`.${className}[data-line="${line}"]`);
}

// Mentre si trascina il cursore: si mostra il numero scelto.
document.addEventListener('input', (event) => {
    const input = event.target;
    if (!(input instanceof HTMLInputElement) || !input.classList.contains('ch11-quantity')) return;
    const value = ch11Find('ch11-quantity-value', input.dataset.line);
    if (value) value.textContent = input.value;
});

// Quando si rilascia il cursore: si manda la nuova quantità al server e si rilegge la tabella.
document.addEventListener('change', async (event) => {
    const input = event.target;
    if (!(input instanceof HTMLInputElement) || !input.classList.contains('ch11-quantity')) return;
    if (!window.fortinoCh11.editor) return;

    const line = Number(input.dataset.line);
    const status = ch11Find('ch11-quantity-status', input.dataset.line);
    if (status) status.textContent = 'invio al server…';

    // La versione vulnerabile non restituisce nulla; quella corretta dice se ha accettato.
    const result = await window.fortinoCh11.editor.invokeMethodAsync('SetQuantity', line, Number(input.value));
    if (status) status.textContent = result === false ? 'rifiutata dal server' : 'inviata al server';

    if (window.fortinoCh11.page) await window.fortinoCh11.page.invokeMethodAsync('Reload');
});
