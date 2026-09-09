import apiClient from './client'
import type { StockTransaction, StockTransactionPayload, UpdateStockTransactionPayload } from '../types/transaction'

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
