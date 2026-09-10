import apiClient from './client'
import type { StockTransaction, StockTransactionPayload, UpdateStockTransactionPayload } from '../types/transaction'

export interface StockTransactionFilters {
  date?: string
  transactionType?: 'Receipt' | 'Issue'
  storeId?: number
  transactionNo?: string
}

function dateRange(date: string): { fromDate: string; toDate: string } {
  return {
    fromDate: `${date}T00:00:00`,
    toDate: `${date}T23:59:59.999`,
  }
}

export async function getStockTransactions(filters: StockTransactionFilters = {}): Promise<StockTransaction[]> {
  const params = {
    ...(filters.date ? dateRange(filters.date) : {}),
    ...(filters.transactionType ? { transactionType: filters.transactionType } : {}),
    ...(filters.storeId ? { storeId: filters.storeId } : {}),
    ...(filters.transactionNo?.trim() ? { transactionNo: filters.transactionNo.trim() } : {}),
  }
  const { data } = await apiClient.get<StockTransaction[]>('/stocktransactions', { params })
  return data
}

export async function createReceiveTransaction(payload: StockTransactionPayload): Promise<void> {
  await apiClient.post('/stocktransactions/receive', payload)
}

export async function createIssueTransaction(payload: StockTransactionPayload): Promise<void> {
  await apiClient.post('/stocktransactions/issue', payload)
}

export async function getStockTransaction(id: number): Promise<StockTransaction> {
  const { data } = await apiClient.get<StockTransaction>(`/stocktransactions/${id}`)
  return data
}

export async function updateStockTransaction(
  id: number,
  payload: UpdateStockTransactionPayload,
): Promise<StockTransaction> {
  const { data } = await apiClient.put<StockTransaction>(`/stocktransactions/${id}`, payload)
  return data
}

export async function deleteStockTransaction(id: number): Promise<void> {
  await apiClient.delete(`/stocktransactions/${id}`)
}
