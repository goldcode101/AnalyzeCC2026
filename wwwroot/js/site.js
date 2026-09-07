// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', () => {
	const dataElement = document.getElementById('transaction-detail-data');
	const detailTitle = document.getElementById('transaction-detail-title');
	const detailCount = document.getElementById('transaction-detail-count');
	const detailTotal = document.getElementById('transaction-detail-total');
	const detailList = document.getElementById('transaction-detail-list');
	const merchantDetailContent = document.getElementById('merchant-detail-content');
	const merchantEmptyState = document.getElementById('merchant-empty-state');
	const merchantDetailName = document.getElementById('merchant-detail-name');
	const merchantDetailCount = document.getElementById('merchant-detail-count');
	const merchantDetailTotal = document.getElementById('merchant-detail-total');
	const merchantMonths = document.getElementById('merchant-months');
	const merchantDetailList = document.getElementById('merchant-detail-list');
	const merchantSearch = document.getElementById('merchant-search');

	if (!dataElement) {
		return;
	}

	const transactions = JSON.parse(dataElement.textContent || '[]');
	const currency = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' });

	const formatMonth = (month) => new Date(`${month}-01T00:00:00`).toLocaleDateString(undefined, {
		month: 'long',
		year: 'numeric'
	});

	const renderMerchantDetails = (merchant, selectedMonth = null) => {
		const merchantTransactions = transactions.filter((transaction) => transaction.Merchant === merchant);
		const matches = selectedMonth
			? merchantTransactions.filter((transaction) => transaction.Month === selectedMonth)
			: merchantTransactions;
		const total = matches.reduce((sum, transaction) => sum + transaction.Amount, 0);
		const byMonth = merchantTransactions.reduce((months, transaction) => {
			months[transaction.Month] = (months[transaction.Month] || 0) + transaction.Amount;
			return months;
		}, {});

		merchantEmptyState.hidden = true;
		merchantDetailContent.hidden = false;
		merchantDetailName.textContent = merchant;
		merchantDetailCount.textContent = `${matches.length} transaction${matches.length === 1 ? '' : 's'}`;
		merchantDetailTotal.textContent = currency.format(total);
		merchantMonths.replaceChildren();
		Object.entries(byMonth).sort(([first], [second]) => second.localeCompare(first)).forEach(([month, monthTotal]) => {
			const monthItem = document.createElement('button');
			monthItem.type = 'button';
			monthItem.className = 'merchant-month';
			if (month === selectedMonth) {
				monthItem.classList.add('is-selected');
			}
			monthItem.setAttribute('aria-pressed', month === selectedMonth ? 'true' : 'false');
			const monthName = document.createElement('span');
			monthName.textContent = formatMonth(month);
			const amount = document.createElement('strong');
			amount.textContent = currency.format(monthTotal);
			monthItem.append(monthName, amount);
			monthItem.addEventListener('click', () => {
				renderMerchantDetails(merchant, selectedMonth === month ? null : month);
			});
			merchantMonths.append(monthItem);
		});

		merchantDetailList.replaceChildren();
		matches.forEach((transaction) => {
			const row = document.createElement('div');
			row.className = 'detail-row';
			if (transaction.IsRefund) {
				row.classList.add('refund-row');
			}
			const description = document.createElement('div');
			const date = document.createElement('strong');
			date.textContent = transaction.Date;
			const metadata = document.createElement('small');
			metadata.textContent = `${transaction.Category}${transaction.IsRefund ? ' | Refund' : ''}`;
			description.append(date, metadata);
			const amount = document.createElement('strong');
			amount.textContent = currency.format(transaction.Amount);
			row.append(description, amount);
			merchantDetailList.append(row);
		});
	};

	const renderDetails = (button) => {
		const month = button.dataset.detailMonth;
		const category = button.dataset.detailCategory;
		const merchant = button.dataset.detailMerchant;
		const matches = transactions.filter((transaction) =>
			transaction.Month === month &&
			(!category || transaction.Category === category) &&
			(!merchant || transaction.Merchant === merchant));
		const total = matches.reduce((sum, transaction) => sum + transaction.Amount, 0);
		const displayMonth = new Date(`${month}-01T00:00:00`).toLocaleDateString(undefined, {
			month: 'long',
			year: 'numeric'
		});

		detailTitle.textContent = merchant
			? `${merchant} in ${displayMonth}`
			: `${category} in ${displayMonth}`;
		detailCount.textContent = `${matches.length} transaction${matches.length === 1 ? '' : 's'}`;
		detailTotal.textContent = currency.format(total);
		detailList.replaceChildren();

		matches.forEach((transaction) => {
			const row = document.createElement('div');
			row.className = 'detail-row';
			if (transaction.IsRefund) {
				row.classList.add('refund-row');
			}

			const description = document.createElement('div');
			const transactionMerchant = document.createElement('strong');
			transactionMerchant.textContent = transaction.Merchant;
			const metadata = document.createElement('small');
			metadata.textContent = `${transaction.Date} | ${transaction.Category}`;
			description.append(transactionMerchant, metadata);

			const amount = document.createElement('strong');
			amount.textContent = currency.format(transaction.Amount);
			row.append(description, amount);
			detailList.append(row);
		});
	};

	document.querySelectorAll('[data-detail-month][data-detail-category], [data-detail-month][data-detail-merchant]').forEach((button) => {
		button.addEventListener('click', () => {
			renderDetails(button);
		});
	});

	document.querySelectorAll('[data-merchant]').forEach((button) => {
		button.addEventListener('click', () => {
			document.querySelectorAll('[data-merchant]').forEach((item) => item.classList.remove('is-selected'));
			button.classList.add('is-selected');
			renderMerchantDetails(button.dataset.merchant);
		});
	});

	merchantSearch?.addEventListener('input', () => {
		const query = merchantSearch.value.trim().toLocaleLowerCase();
		document.querySelectorAll('[data-merchant]').forEach((button) => {
			button.hidden = !button.dataset.merchant.toLocaleLowerCase().includes(query);
		});
	});
});
