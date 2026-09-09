export type TransactionType = 'Receive' | 'Issue'

export interface TransactionDetailPayload {
  itemId: number
  quantity: number
  unit: string
  remarks?: string | null
}

export interface StockTransactionPayload {
  transactionNo: string
  transactionDate?: string | null
  storeId: number
  remarks?: string | null
  details: TransactionDetailPayload[]
}
