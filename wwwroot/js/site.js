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
	const rawDataElement = document.getElementById('raw-transaction-data');
	const formatMonth = (month) => new Date(`${month}-01T00:00:00`).toLocaleDateString(undefined, {
		month: 'long',
		year: 'numeric'
	});

	if (rawDataElement) {
		const rawTransactions = JSON.parse(rawDataElement.textContent || '[]');
		const rawBody = document.getElementById('raw-transactions-body');
		const rawSearch = document.getElementById('raw-search');
		const rawMonth = document.getElementById('raw-month');
		const rawCategory = document.getElementById('raw-category');
		const rawType = document.getElementById('raw-type');
		const rawClear = document.getElementById('raw-clear-filters');
		const rawResultCount = document.getElementById('raw-result-count');
		const rawTotalDebit = document.getElementById('raw-total-debit');
		const rawTotalCredit = document.getElementById('raw-total-credit');
		const rawTotalExcluded = document.getElementById('raw-total-excluded');
		const rawTotalNet = document.getElementById('raw-total-net');
		const rawState = { sort: 'date', descending: true };
		const rawSortProperties = {
			date: 'DateValue',
			postedDate: 'PostedDateValue',
			description: 'Description',
			originalCategory: 'OriginalCategory',
			effectiveCategory: 'EffectiveCategory',
			debit: 'Debit',
			credit: 'Credit',
			netAmount: 'NetAmount'
		};
		const rawCurrency = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' });

		const rawTypeFor = (transaction) => transaction.IsExcludedPayment
			? 'card-payment'
			: transaction.Credit > 0
				? 'credit'
				: 'charge';

		const rawTypeLabel = (type) => ({
			'card-payment': 'Card payment',
			credit: 'Credit',
			charge: 'Charge'
		}[type]);

		const rawMonthKey = (dateValue) => dateValue.substring(0, 7);

		const addOptions = (select, values, labelFormatter) => {
			[...new Set(values)].sort().forEach((value) => {
				const option = document.createElement('option');
				option.value = value;
				option.textContent = labelFormatter ? labelFormatter(value) : value;
				select.append(option);
			});
		};

		addOptions(rawMonth, rawTransactions.map((transaction) => rawMonthKey(transaction.DateValue)), (month) => formatMonth(month));
		addOptions(rawCategory, rawTransactions.map((transaction) => transaction.EffectiveCategory));

		const renderRawTransactions = () => {
			const query = rawSearch.value.trim().toLocaleLowerCase();
			const filtered = rawTransactions
				.filter((transaction) => {
					const searchable = `${transaction.Description} ${transaction.OriginalCategory} ${transaction.EffectiveCategory}`.toLocaleLowerCase();
					const type = rawTypeFor(transaction);
					return (!query || searchable.includes(query)) &&
						(!rawMonth.value || rawMonthKey(transaction.DateValue) === rawMonth.value) &&
						(!rawCategory.value || transaction.EffectiveCategory === rawCategory.value) &&
						(!rawType.value || type === rawType.value);
				})
				.sort((first, second) => {
					const sortProperty = rawSortProperties[rawState.sort];
					const firstValue = first[sortProperty];
					const secondValue = second[sortProperty];
					const comparison = typeof firstValue === 'string'
						? firstValue.localeCompare(secondValue)
						: firstValue - secondValue;
					return rawState.descending ? -comparison : comparison;
				});

			rawBody.replaceChildren();
			filtered.forEach((transaction) => {
				const row = document.createElement('tr');
				const type = rawTypeFor(transaction);
				const cells = [
					transaction.Date,
					transaction.PostedDate,
					transaction.Description,
					transaction.OriginalCategory,
					transaction.EffectiveCategory,
					rawCurrency.format(transaction.Debit),
					rawCurrency.format(transaction.Credit),
					rawCurrency.format(transaction.NetAmount)
				];

				cells.forEach((value, index) => {
					const cell = document.createElement('td');
					cell.textContent = value;
					if (index >= 5) {
						cell.className = `text-end ${index === 7 ? (transaction.NetAmount >= 0 ? 'amount-positive' : 'amount-negative') : ''}`;
					}
					row.append(cell);
				});

				const typeCell = document.createElement('td');
				const typeBadge = document.createElement('span');
				typeBadge.className = `transaction-type transaction-type-${type}`;
				typeBadge.textContent = rawTypeLabel(type);
				typeCell.append(typeBadge);
				row.append(typeCell);
				rawBody.append(row);
			});

			rawResultCount.textContent = `${filtered.length} of ${rawTransactions.length} records`;
			rawTotalDebit.textContent = rawCurrency.format(filtered.reduce((sum, transaction) => sum + transaction.Debit, 0));
			rawTotalCredit.textContent = rawCurrency.format(filtered
				.filter((transaction) => !transaction.IsExcludedPayment)
				.reduce((sum, transaction) => sum + transaction.Credit, 0));
			rawTotalExcluded.textContent = rawCurrency.format(filtered
				.filter((transaction) => transaction.IsExcludedPayment)
				.reduce((sum, transaction) => sum + transaction.Credit, 0));
			rawTotalNet.textContent = rawCurrency.format(filtered
				.filter((transaction) => !transaction.IsExcludedPayment)
				.reduce((sum, transaction) => sum + transaction.NetAmount, 0));
		};

		document.querySelectorAll('[data-raw-sort]').forEach((button) => {
			button.addEventListener('click', () => {
				const sort = button.dataset.rawSort;
				if (rawState.sort === sort) {
					rawState.descending = !rawState.descending;
				} else {
					rawState.sort = sort;
					rawState.descending = false;
				}
				document.querySelectorAll('[data-raw-sort]').forEach((item) => {
					item.classList.toggle('is-active', item === button);
					item.setAttribute('aria-sort', item === button ? (rawState.descending ? 'descending' : 'ascending') : 'none');
				});
				renderRawTransactions();
			});
		});

		[rawSearch, rawMonth, rawCategory, rawType].forEach((control) => {
			control.addEventListener('input', renderRawTransactions);
			control.addEventListener('change', renderRawTransactions);
		});

		rawClear.addEventListener('click', () => {
			rawSearch.value = '';
			rawMonth.value = '';
			rawCategory.value = '';
			rawType.value = '';
			rawState.sort = 'date';
			rawState.descending = true;
			renderRawTransactions();
		});

		renderRawTransactions();
	}

	if (!dataElement) {
		return;
	}

	const transactions = JSON.parse(dataElement.textContent || '[]');
	const currency = new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' });

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
