import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import type { StockTransaction } from '../types/transaction'
import type { Store } from '../types/store'
import { deleteStockTransaction, getStockTransactions } from '../api/transactions'
import { getStores } from '../api/stores'
import { extractError } from '../api/errors'
import Modal from '../components/Modal'

interface FilterState {
  date: string
  transactionType: '' | 'Receipt' | 'Issue'
  storeId: string
  transactionNo: string
}

const initialFilters: FilterState = { date: '', transactionType: '', storeId: '', transactionNo: '' }

function formatDate(value: string): string {
  const [datePart] = value.split('T')
  const parsed = new Date(`${datePart}T00:00:00`)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleDateString()
}

function formatTransactionType(value: StockTransaction['transactionType']): string {
  return value === 'Receipt' ? 'Receive' : value === 'Issue' ? 'Issue' : value
}

export default function StockTransactionsPage() {
  const [transactions, setTransactions] = useState<StockTransaction[]>([])
  const [stores, setStores] = useState<Store[]>([])
  const [filters, setFilters] = useState<FilterState>(initialFilters)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [viewing, setViewing] = useState<StockTransaction | null>(null)

  async function load(nextFilters = filters) {
    setLoading(true)
    setError(null)
    try {
      const [transactionList, storeList] = await Promise.all([
        getStockTransactions({
          date: nextFilters.date || undefined,
          transactionType: nextFilters.transactionType || undefined,
          storeId: nextFilters.storeId ? Number(nextFilters.storeId) : undefined,
          transactionNo: nextFilters.transactionNo,
        }),
        stores.length === 0 ? getStores() : Promise.resolve(stores),
      ])
      setTransactions(transactionList)
      if (stores.length === 0) setStores(storeList)
    } catch (err) {
      setError(extractError(err))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  function updateFilter<K extends keyof FilterState>(field: K, value: FilterState[K]) {
    setFilters((current) => ({ ...current, [field]: value }))
  }

  async function handleDelete(transaction: StockTransaction) {
    if (!window.confirm(`Delete transaction '${transaction.transactionNo}'?`)) return
    setError(null)
    setMessage(null)
    try {
      await deleteStockTransaction(transaction.id)
      setMessage(`Transaction '${transaction.transactionNo}' deleted.`)
      await load()
    } catch (err) {
      setError(extractError(err))
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Stock Transactions</h1>
        <Link className="button primary" to="/stock-transactions/new">
          + New Transaction
        </Link>
      </div>

      <form
        className="filters"
        onSubmit={(event) => {
          event.preventDefault()
          load()
        }}
      >
        <div className="filter-field">
          <label htmlFor="transaction-date">Date</label>
          <input id="transaction-date" type="date" value={filters.date} onChange={(event) => updateFilter('date', event.target.value)} />
        </div>
        <div className="filter-field">
          <label htmlFor="transaction-type">Transaction Type</label>
          <select id="transaction-type" value={filters.transactionType} onChange={(event) => updateFilter('transactionType', event.target.value as FilterState['transactionType'])}>
            <option value="">All types</option>
            <option value="Receipt">Receive</option>
            <option value="Issue">Issue</option>
          </select>
        </div>
        <div className="filter-field">
          <label htmlFor="transaction-store">Store</label>
          <select id="transaction-store" value={filters.storeId} onChange={(event) => updateFilter('storeId', event.target.value)}>
            <option value="">All stores</option>
            {stores.map((store) => <option key={store.id} value={store.id}>{store.code} - {store.name}</option>)}
          </select>
        </div>
        <div className="filter-field">
          <label htmlFor="transaction-no">Transaction No</label>
          <input id="transaction-no" value={filters.transactionNo} onChange={(event) => updateFilter('transactionNo', event.target.value)} />
        </div>
        <button className="primary" type="submit">Filter</button>
      </form>

      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      {loading ? <p>Loading transactions...</p> : transactions.length === 0 ? <p>No transactions found.</p> : (
        <table className="data-table">
          <thead><tr><th>Transaction No</th><th>Transaction Date</th><th>Transaction Type</th><th>Store</th><th>Remarks</th><th>Actions</th></tr></thead>
          <tbody>
            {transactions.map((transaction) => (
              <tr key={transaction.id}>
                <td>{transaction.transactionNo}</td>
                <td>{formatDate(transaction.transactionDate)}</td>
                <td>{formatTransactionType(transaction.transactionType)}</td>
                <td>{transaction.storeCode} - {transaction.storeName}</td>
                <td>{transaction.remarks || '-'}</td>
                <td className="actions">
                  <button type="button" onClick={() => setViewing(transaction)}>View</button>
                  <Link className="button" to={`/stock-transactions/${transaction.id}/edit`}>Edit</Link>
                  <button type="button" onClick={() => handleDelete(transaction)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <Modal open={viewing !== null} title={viewing ? `Transaction ${viewing.transactionNo}` : ''} onClose={() => setViewing(null)}>
        {viewing && (
          <div>
            <p><strong>Date:</strong> {formatDate(viewing.transactionDate)}</p>
            <p><strong>Type:</strong> {formatTransactionType(viewing.transactionType)}</p>
            <p><strong>Store:</strong> {viewing.storeCode} - {viewing.storeName}</p>
            <p><strong>Remarks:</strong> {viewing.remarks || '-'}</p>
            <table className="data-table">
              <thead><tr><th>Item</th><th>Quantity</th><th>Unit</th><th>Remarks</th></tr></thead>
              <tbody>{viewing.details.map((detail) => <tr key={detail.id}><td>{detail.itemCode} - {detail.itemName}</td><td>{detail.quantity}</td><td>{detail.unit}</td><td>{detail.remarks || '-'}</td></tr>)}</tbody>
            </table>
          </div>
        )}
      </Modal>
    </div>
  )
}
