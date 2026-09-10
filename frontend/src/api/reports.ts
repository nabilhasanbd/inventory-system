import apiClient from './client'
import type { StockMovementReportRow, TransactionDetailReportRow } from '../types/report'

export interface StockMovementReportFilters {
  fromDate?: string
  toDate?: string
  storeId?: number
  itemId?: number
}

export interface TransactionDetailReportFilters extends StockMovementReportFilters {
  transactionType?: 'Receipt' | 'Issue' | 'OpeningBalance'
}

export interface ReportFileResult {
  blob: Blob
  fileName: string
  contentType: string
}

export async function getStockMovementReport(filters: StockMovementReportFilters = {}): Promise<StockMovementReportRow[]> {
  const { data } = await apiClient.get<StockMovementReportRow[]>('/stockreports/movement', {
    params: buildParams(filters),
  })

  return data
}

export async function exportStockMovementReport(
  filters: StockMovementReportFilters = {},
  format: 'PDF' | 'XLSX' = 'PDF',
): Promise<ReportFileResult> {
  const response = await apiClient.get<Blob>('/stockreports/movement/export', {
    params: {
      ...buildParams(filters),
      format,
    },
    responseType: 'blob',
  })

  return {
    blob: response.data,
    contentType: String(response.headers['content-type'] ?? 'application/octet-stream'),
    fileName: parseFileName(typeof response.headers['content-disposition'] === 'string' ? response.headers['content-disposition'] : undefined, format),
  }
}

export async function getTransactionDetailReport(filters: TransactionDetailReportFilters = {}): Promise<TransactionDetailReportRow[]> {
  const { data } = await apiClient.get<TransactionDetailReportRow[]>('/stockreports/transaction-details', {
    params: buildParams(filters),
  })

  return data
}

export async function exportTransactionDetailReport(
  filters: TransactionDetailReportFilters = {},
  format: 'PDF' | 'XLSX' = 'PDF',
): Promise<ReportFileResult> {
  const response = await apiClient.get<Blob>('/stockreports/transaction-details/export', {
    params: {
      ...buildParams(filters),
      format,
    },
    responseType: 'blob',
  })

  return {
    blob: response.data,
    contentType: String(response.headers['content-type'] ?? 'application/octet-stream'),
    fileName: parseFileName(
      typeof response.headers['content-disposition'] === 'string' ? response.headers['content-disposition'] : undefined,
      format,
      'transaction-detail',
    ),
  }
}

function buildParams(filters: StockMovementReportFilters | TransactionDetailReportFilters) {
  return {
    ...(filters.fromDate ? { fromDate: filters.fromDate } : {}),
    ...(filters.toDate ? { toDate: filters.toDate } : {}),
    ...(filters.storeId ? { storeId: filters.storeId } : {}),
    ...(filters.itemId ? { itemId: filters.itemId } : {}),
    ...('transactionType' in filters && filters.transactionType ? { transactionType: filters.transactionType } : {}),
  }
}

function parseFileName(header: string | undefined, format: 'PDF' | 'XLSX', fallback = 'stock-movement'): string {
  const match = header?.match(/filename="?([^"]+)"?/)
  return match?.[1] ?? `${fallback}.${format.toLowerCase()}`
}
