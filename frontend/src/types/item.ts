export interface Item {
  id: number
  itemCode: string
  itemName: string
  category: string | null
  unit: string
  reorderLevel: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateItemPayload {
  itemCode: string
  itemName: string
  category: string
  unit: string
  reorderLevel: number
  isActive: boolean
}

export interface UpdateItemPayload {
  itemCode: string
  itemName: string
  category: string
  unit: string
  reorderLevel: number
}
