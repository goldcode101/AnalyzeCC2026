// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', () => {
	const dataElement = document.getElementById('transaction-detail-data');
	const detailTitle = document.getElementById('transaction-detail-title');
	const detailCount = document.getElementById('transaction-detail-count');
	const detailTotal = document.getElementById('transaction-detail-total');
	const detailList = document.getElementById('transaction-detail-list');

	if (!dataElement || !detailTitle || !detailCount || !detailTotal || !detailList) {
		return;
	}

	const transactions = JSON.parse(dataElement.textContent || '[]');
	const currency = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' });

	document.querySelectorAll('[data-detail-month][data-detail-category]').forEach((button) => {
		button.addEventListener('click', () => {
			const month = button.dataset.detailMonth;
			const category = button.dataset.detailCategory;
			const matches = transactions.filter((transaction) =>
				transaction.Month === month && transaction.Category === category);
			const total = matches.reduce((sum, transaction) => sum + transaction.Amount, 0);
			const displayMonth = new Date(`${month}-01T00:00:00`).toLocaleDateString(undefined, {
				month: 'long',
				year: 'numeric'
			});

			detailTitle.textContent = `${category} in ${displayMonth}`;
			detailCount.textContent = `${matches.length} transaction${matches.length === 1 ? '' : 's'}`;
			detailTotal.textContent = currency.format(total);
			detailList.replaceChildren();

			matches.forEach((transaction) => {
				const row = document.createElement('div');
				row.className = 'detail-row';

				const description = document.createElement('div');
				const merchant = document.createElement('strong');
				merchant.textContent = transaction.Merchant;
				const metadata = document.createElement('small');
				metadata.textContent = `${transaction.Date} | Original: ${transaction.OriginalCategory}`;
				description.append(merchant, metadata);

				const amount = document.createElement('strong');
				amount.textContent = currency.format(transaction.Amount);
				row.append(description, amount);
				detailList.append(row);
			});
		});
	});
});
