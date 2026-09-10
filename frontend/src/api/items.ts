import apiClient from './client'
import type { Item, CreateItemPayload, UpdateItemPayload } from '../types/item'

export async function getItems(): Promise<Item[]> {
  const { data } = await apiClient.get<Item[]>('/items')
  return data
}

export async function createItem(payload: CreateItemPayload): Promise<Item> {
  const { data } = await apiClient.post<Item>('/items', payload)
  return data
}

export async function updateItem(id: number, payload: UpdateItemPayload): Promise<Item> {
  const { data } = await apiClient.put<Item>(`/items/${id}`, payload)
  return data
}

export async function deleteItem(id: number): Promise<void> {
  await apiClient.delete(`/items/${id}`)
}

export async function setItemStatus(id: number, isActive: boolean): Promise<Item> {
  const action = isActive ? 'activate' : 'deactivate'
  const { data } = await apiClient.patch<Item>(`/items/${id}/${action}`)
  return data
}
