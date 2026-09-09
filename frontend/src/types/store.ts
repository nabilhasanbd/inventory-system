export interface Store {
  id: number
  code: string
  name: string
  isActive: boolean
}

export interface CreateStorePayload {
  code: string
  name: string
  isActive: boolean
}

export interface UpdateStorePayload {
  code: string
  name: string
}
