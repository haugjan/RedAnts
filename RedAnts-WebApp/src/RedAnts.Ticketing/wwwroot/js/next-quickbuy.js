(function () {
    var form = document.getElementById('nqForm');
    if (!form) return;

    var rows = Array.prototype.slice.call(form.querySelectorAll('[data-nq-row]'));
    if (rows.length === 0) return;

    var buy = document.getElementById('nqBuy');
    var label = document.getElementById('nqBuyLabel');
    var detail = document.getElementById('nqBuyDetail');
    var max = parseInt(form.getAttribute('data-nq-max'), 10) || 9;

    function limitOf(row) {
        var limit = parseInt(row.getAttribute('data-nq-limit'), 10);
        return isNaN(limit) ? max : Math.min(max, limit);
    }

    function valueOf(row) {
        var input = row.querySelector('[data-nq-input]');
        var parsed = parseInt(input.value, 10);
        var value = isNaN(parsed) ? 0 : parsed;
        return Math.max(0, Math.min(value, limitOf(row)));
    }

    function money(amount) {
        return 'CHF ' + amount.toFixed(2);
    }

    function render() {
        var total = 0;
        var count = 0;
        var parts = [];

        rows.forEach(function (row) {
            var input = row.querySelector('[data-nq-input]');
            var value = valueOf(row);
            if (input.value !== String(value)) input.value = String(value);

            var price = parseFloat(row.getAttribute('data-nq-price')) || 0;
            total += value * price;
            count += value;
            if (value > 0) parts.push(value + ' × ' + row.getAttribute('data-nq-name'));

            if (value > 0) row.classList.add('nq-cat--active');
            else row.classList.remove('nq-cat--active');

            if (value > 0) input.classList.remove('nq-step__value--zero');
            else input.classList.add('nq-step__value--zero');

            var minus = row.querySelector('[data-nq-step="-1"]');
            if (minus) minus.disabled = value <= 0;
            var plus = row.querySelector('[data-nq-step="1"]');
            if (plus) plus.disabled = value >= limitOf(row);
        });

        if (label) label.textContent = count === 0 ? 'Anzahl wählen' : 'Kaufen · ' + money(total);
        if (detail) detail.textContent = count === 0 ? 'noch nichts gewählt' : parts.join(', ');
        if (buy) buy.disabled = count === 0;
    }

    form.addEventListener('click', function (event) {
        var button = event.target.closest('[data-nq-step]');
        if (!button) return;
        var row = button.closest('[data-nq-row]');
        if (!row) return;
        var input = row.querySelector('[data-nq-input]');
        var step = parseInt(button.getAttribute('data-nq-step'), 10) || 0;
        input.value = String(Math.max(0, Math.min(valueOf(row) + step, limitOf(row))));
        render();
    });

    form.addEventListener('input', function (event) {
        if (event.target.matches('[data-nq-input]')) render();
    });

    form.addEventListener('change', function (event) {
        if (event.target.matches('[data-nq-input]')) render();
    });

    render();
}());
