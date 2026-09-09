import apiClient from './client'
import type { StockTransactionPayload } from '../types/transaction'

export async function createReceiveTransaction(payload: StockTransactionPayload): Promise<void> {
  await apiClient.post('/stocktransactions/receive', payload)
}

export async function createIssueTransaction(payload: StockTransactionPayload): Promise<void> {
  await apiClient.post('/stocktransactions/issue', payload)
}
