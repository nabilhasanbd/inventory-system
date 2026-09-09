import apiClient from './client'

export async function getAvailableStock(storeId: number, itemId: number): Promise<number> {
  const { data } = await apiClient.get<{ quantity: number }>('/stockbalances', {
    params: { storeId, itemId },
  })
  return data.quantity
}
