import { useEffect, useState } from 'react'
import { extractError } from '../api/errors'
import { exportStockMovementReport, getStockMovementReport } from '../api/reports'
import { getItems } from '../api/items'
import { getStores } from '../api/stores'
import type { Item } from '../types/item'
import type { StockMovementReportRow } from '../types/report'
import type { Store } from '../types/store'

interface FilterState {
  fromDate: string
  toDate: string
  storeId: string
  itemId: string
}

const initialFilters: FilterState = {
  fromDate: '',
  toDate: '',
  storeId: '',
  itemId: '',
}

function toParams(filters: FilterState) {
  return {
    fromDate: filters.fromDate || undefined,
    toDate: filters.toDate || undefined,
    storeId: filters.storeId ? Number(filters.storeId) : undefined,
    itemId: filters.itemId ? Number(filters.itemId) : undefined,
  }
}

function formatNumber(value: number): string {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export default function StockReportsPage() {
  const [filters, setFilters] = useState<FilterState>(initialFilters)
  const [rows, setRows] = useState<StockMovementReportRow[]>([])
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
      const reportRows = await getStockMovementReport(toParams(filters))
      setRows(reportRows)
      setMessage(reportRows.length === 0 ? 'No stock movement found for the selected filters.' : `Generated ${reportRows.length} report row${reportRows.length === 1 ? '' : 's'}.`)
    } catch (err) {
      setRows([])
      setError(extractError(err))
    } finally {
      setLoading(false)
    }
  }

  async function handleExport() {
    setExporting(true)
    setError(null)

    try {
      const file = await exportStockMovementReport(toParams(filters), 'XLSX')
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
      const file = await exportStockMovementReport(toParams(filters), 'PDF')
      printFile(file.blob)
    } catch (err) {
      setError(extractError(err))
    } finally {
      setPrinting(false)
    }
  }

  const totals = rows.reduce(
    (summary, row) => ({
      opening: summary.opening + row.opening,
      receive: summary.receive + row.receive,
      issue: summary.issue + row.issue,
      return: summary.return + row.return,
      closing: summary.closing + row.closing,
    }),
    { opening: 0, receive: 0, issue: 0, return: 0, closing: 0 },
  )

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <h1>Stock Movement Report</h1>
          <p className="page-subtitle">Review opening, receive, issue, return, and closing balances from transaction history.</p>
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
          <label htmlFor="report-from-date">From Date</label>
          <input id="report-from-date" type="date" value={filters.fromDate} onChange={(event) => updateFilter('fromDate', event.target.value)} />
        </div>
        <div className="filter-field">
          <label htmlFor="report-to-date">To Date</label>
          <input id="report-to-date" type="date" value={filters.toDate} onChange={(event) => updateFilter('toDate', event.target.value)} />
        </div>
        <div className="filter-field">
          <label htmlFor="report-store">Store</label>
          <select id="report-store" value={filters.storeId} onChange={(event) => updateFilter('storeId', event.target.value)} disabled={bootLoading}>
            <option value="">All stores</option>
            {stores.map((store) => <option key={store.id} value={store.id}>{store.code} - {store.name}</option>)}
          </select>
        </div>
        <div className="filter-field">
          <label htmlFor="report-item">Item</label>
          <select id="report-item" value={filters.itemId} onChange={(event) => updateFilter('itemId', event.target.value)} disabled={bootLoading}>
            <option value="">All items</option>
            {items.map((item) => <option key={item.id} value={item.id}>{item.itemCode} - {item.itemName}</option>)}
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
          <span className="report-card-label">Opening</span>
          <strong>{formatNumber(totals.opening)}</strong>
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
          <span className="report-card-label">Closing</span>
          <strong>{formatNumber(totals.closing)}</strong>
        </div>
      </div>

      {loading ? <p>Generating report...</p> : (
        <div className="report-table-shell">
          <table className="data-table report-table">
            <thead>
              <tr>
                <th>Item</th>
                <th>Store</th>
                <th>Opening</th>
                <th>Receive</th>
                <th>Issue</th>
                <th>Return</th>
                <th>Closing</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr>
                  <td colSpan={7} className="report-empty">Run the report to see stock movement.</td>
                </tr>
              ) : rows.map((row) => (
                <tr key={`${row.itemId}-${row.storeId}`}>
                  <td>
                    <div>{row.item}</div>
                    <small>{row.unit}</small>
                  </td>
                  <td>{row.store}</td>
                  <td className="numeric-cell">{formatNumber(row.opening)}</td>
                  <td className="numeric-cell">{formatNumber(row.receive)}</td>
                  <td className="numeric-cell">{formatNumber(row.issue)}</td>
                  <td className="numeric-cell">{formatNumber(row.return)}</td>
                  <td className="numeric-cell"><strong>{formatNumber(row.closing)}</strong></td>
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
