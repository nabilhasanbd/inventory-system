export interface StockMovementReportRow {
  itemId: number
  itemCode: string
  itemName: string
  item: string
  storeId: number
  storeCode: string
  storeName: string
  store: string
  unit: string
  opening: number
  receive: number
  issue: number
  return: number
  closing: number
}

export interface TransactionDetailReportRow {
  detailId: number
  transactionDate: string
  transactionNo: string
  transactionType: string
  storeId: number
  storeCode: string
  storeName: string
  store: string
  itemId: number
  itemCode: string
  itemName: string
  item: string
  openingQuantity: number
  receiveQuantity: number
  issueQuantity: number
  closingQuantity: number
}
