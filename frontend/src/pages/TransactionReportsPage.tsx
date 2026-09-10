import { useEffect, useState } from 'react'
import { extractError } from '../api/errors'
import { exportTransactionDetailReport, getTransactionDetailReport } from '../api/reports'
import { getItems } from '../api/items'
import { getStores } from '../api/stores'
import type { Item } from '../types/item'
import type { TransactionDetailReportRow } from '../types/report'
import type { Store } from '../types/store'

interface FilterState {
  fromDate: string
  toDate: string
  storeId: string
  itemId: string
  transactionType: '' | 'Receipt' | 'Issue' | 'OpeningBalance'
}

const initialFilters: FilterState = {
  fromDate: '',
  toDate: '',
  storeId: '',
  itemId: '',
  transactionType: '',
}

function toParams(filters: FilterState) {
  return {
    fromDate: filters.fromDate || undefined,
    toDate: filters.toDate || undefined,
    storeId: filters.storeId ? Number(filters.storeId) : undefined,
    itemId: filters.itemId ? Number(filters.itemId) : undefined,
    transactionType: filters.transactionType || undefined,
  }
}

function formatNumber(value: number): string {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

function formatDate(value: string): string {
  const [datePart] = value.split('T')
  const parsed = new Date(`${datePart}T00:00:00`)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleDateString()
}

export default function TransactionReportsPage() {
  const [filters, setFilters] = useState<FilterState>(initialFilters)
  const [rows, setRows] = useState<TransactionDetailReportRow[]>([])
  const [stores, setStores] = useState<Store[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [loading, setLoading] = useState(false)
  const [bootLoading, setBootLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [printing, setPrinting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    Promise.all([getStores(), getItems()])
      .then(([storeList, itemList]) => {
        setStores(storeList)
        setItems(itemList)
      })
      .catch((err) => setError(extractError(err)))
      .finally(() => setBootLoading(false))
  }, [])

  function updateFilter<K extends keyof FilterState>(field: K, value: FilterState[K]) {
    setFilters((current) => ({ ...current, [field]: value }))
  }

  async function handleGenerate() {
    setLoading(true)
    setError(null)
    setMessage(null)

    try {
      const reportRows = await getTransactionDetailReport(toParams(filters))
      setRows(reportRows)
      setMessage(reportRows.length === 0 ? 'No transaction details found for the selected filters.' : `Generated ${reportRows.length} report row${reportRows.length === 1 ? '' : 's'}.`)
    } catch (err) {
      setError(extractError(err))
    } finally {
      setLoading(false)
    }
  }

  async function handleExport() {
    setExporting(true)
    setError(null)

    try {
      const file = await exportTransactionDetailReport(toParams(filters), 'XLSX')
      downloadFile(file.blob, file.fileName)
    } catch (err) {
      setError(extractError(err))
    } finally {
      setExporting(false)
    }
  }

  async function handlePrint() {
    setPrinting(true)
    setError(null)

    try {
      const file = await exportTransactionDetailReport(toParams(filters), 'PDF')
      printFile(file.blob)
    } catch (err) {
      setError(extractError(err))
    } finally {
      setPrinting(false)
    }
  }

  const totals = rows.reduce(
    (summary, row) => ({
      receive: summary.receive + row.receiveQuantity,
      issue: summary.issue + row.issueQuantity,
      closing: summary.closing + row.closingQuantity,
    }),
    { receive: 0, issue: 0, closing: 0 },
  )

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Transaction Detail Report</h1>
          <p className="page-subtitle">Review transaction-level opening, receive, issue, and closing quantities from the existing report API.</p>
        </div>
      </div>

      <form
        className="filters"
        onSubmit={(event) => {
          event.preventDefault()
          handleGenerate()
        }}
      >
        <div className="filter-field">
          <label htmlFor="transaction-report-from-date">From Date</label>
          <input id="transaction-report-from-date" type="date" value={filters.fromDate} onChange={(event) => updateFilter('fromDate', event.target.value)} />
        </div>
        <div className="filter-field">
          <label htmlFor="transaction-report-to-date">To Date</label>
          <input id="transaction-report-to-date" type="date" value={filters.toDate} onChange={(event) => updateFilter('toDate', event.target.value)} />
        </div>
        <div className="filter-field">
          <label htmlFor="transaction-report-store">Store</label>
          <select id="transaction-report-store" value={filters.storeId} onChange={(event) => updateFilter('storeId', event.target.value)} disabled={bootLoading}>
            <option value="">All stores</option>
            {stores.map((store) => <option key={store.id} value={store.id}>{store.code} - {store.name}</option>)}
          </select>
        </div>
        <div className="filter-field">
          <label htmlFor="transaction-report-item">Item</label>
          <select id="transaction-report-item" value={filters.itemId} onChange={(event) => updateFilter('itemId', event.target.value)} disabled={bootLoading}>
            <option value="">All items</option>
            {items.map((item) => <option key={item.id} value={item.id}>{item.itemCode} - {item.itemName}</option>)}
          </select>
        </div>
        <div className="filter-field">
          <label htmlFor="transaction-report-type">Transaction Type</label>
          <select id="transaction-report-type" value={filters.transactionType} onChange={(event) => updateFilter('transactionType', event.target.value as FilterState['transactionType'])}>
            <option value="">All types</option>
            <option value="Receipt">Receive</option>
            <option value="Issue">Issue</option>
            <option value="OpeningBalance">Opening Balance</option>
          </select>
        </div>
        <button className="primary" type="submit" disabled={loading || bootLoading}>Generate</button>
        <button type="button" onClick={handlePrint} disabled={printing || bootLoading}>Print</button>
        <button type="button" onClick={handleExport} disabled={exporting || bootLoading}>Export</button>
      </form>

      {message && <div className="alert alert-success">{message}</div>}
      {error && <div className="alert alert-error">{error}</div>}

      <div className="report-summary">
        <div className="report-card">
          <span className="report-card-label">Rows</span>
          <strong>{rows.length}</strong>
        </div>
        <div className="report-card">
          <span className="report-card-label">Receive</span>
          <strong>{formatNumber(totals.receive)}</strong>
        </div>
        <div className="report-card">
          <span className="report-card-label">Issue</span>
          <strong>{formatNumber(totals.issue)}</strong>
        </div>
        <div className="report-card">
          <span className="report-card-label">Last Closing</span>
          <strong>{rows.length === 0 ? formatNumber(0) : formatNumber(rows[rows.length - 1].closingQuantity)}</strong>
        </div>
        <div className="report-card">
          <span className="report-card-label">Closing Total</span>
          <strong>{formatNumber(totals.closing)}</strong>
        </div>
      </div>

      {loading ? <p>Generating report...</p> : (
        <div className="report-table-shell">
          <table className="data-table report-table">
            <thead>
              <tr>
                <th>Transaction Date</th>
                <th>Transaction No</th>
                <th>Transaction Type</th>
                <th>Store</th>
                <th>Item</th>
                <th>Opening Quantity</th>
                <th>Receive Quantity</th>
                <th>Issue Quantity</th>
                <th>Closing Quantity</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr>
                  <td colSpan={9} className="report-empty">Run the report to see transaction details.</td>
                </tr>
              ) : rows.map((row) => (
                <tr key={`${row.transactionNo}-${row.itemId}-${row.storeId}`}>
                  <td>{formatDate(row.transactionDate)}</td>
                  <td>{row.transactionNo}</td>
                  <td>{row.transactionType === 'Receipt' ? 'Receive' : row.transactionType === 'OpeningBalance' ? 'Opening Balance' : row.transactionType}</td>
                  <td>{row.store}</td>
                  <td>{row.item}</td>
                  <td className="numeric-cell">{formatNumber(row.openingQuantity)}</td>
                  <td className="numeric-cell">{formatNumber(row.receiveQuantity)}</td>
                  <td className="numeric-cell">{formatNumber(row.issueQuantity)}</td>
                  <td className="numeric-cell"><strong>{formatNumber(row.closingQuantity)}</strong></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function downloadFile(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

function printFile(blob: Blob) {
  const url = URL.createObjectURL(blob)
  const frame = document.createElement('iframe')
  frame.style.display = 'none'
  frame.src = url
  frame.onload = () => {
    frame.contentWindow?.focus()
    frame.contentWindow?.print()
    window.setTimeout(() => {
      frame.remove()
      URL.revokeObjectURL(url)
    }, 1000)
  }
  document.body.appendChild(frame)
}
