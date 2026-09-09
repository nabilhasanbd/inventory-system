import apiClient from './client'
import type { Store, CreateStorePayload, UpdateStorePayload } from '../types/store'

export async function getStores(): Promise<Store[]> {
  const { data } = await apiClient.get<Store[]>('/stores')
  return data
}

export async function createStore(payload: CreateStorePayload): Promise<Store> {
  const { data } = await apiClient.post<Store>('/stores', payload)
  return data
}

export async function updateStore(id: number, payload: UpdateStorePayload): Promise<Store> {
  const { data } = await apiClient.put<Store>(`/stores/${id}`, payload)
  return data
}

export async function setStoreStatus(id: number, isActive: boolean): Promise<Store> {
  const action = isActive ? 'activate' : 'deactivate'
  const { data } = await apiClient.patch<Store>(`/stores/${id}/${action}`)
  return data
}
