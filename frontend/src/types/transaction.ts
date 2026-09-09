export type TransactionType = 'Receive' | 'Issue'

export interface TransactionDetailPayload {
  itemId: number
  quantity: number
  unit: string
  remarks?: string | null
}

export interface UpdateTransactionDetailPayload extends TransactionDetailPayload {
  id: number
}

export interface StockTransactionPayload {
  transactionNo: string
  transactionDate?: string | null
  storeId: number
  remarks?: string | null
  details: TransactionDetailPayload[]
}

export interface UpdateStockTransactionPayload {
  transactionDate?: string | null
  remarks?: string | null
  details: UpdateTransactionDetailPayload[]
}

export interface StockTransactionDetail {
  id: number
  itemId: number
  itemCode: string
  itemName: string
  quantity: number
  unit: string
  remarks: string | null
}

export interface StockTransaction {
  id: number
  transactionNo: string
  transactionDate: string
  transactionType: 'Receipt' | 'Issue'
  storeId: number
  storeCode: string
  storeName: string
  remarks: string | null
  details: StockTransactionDetail[]
}
