const base = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/?$/, '')

function buildUrl(path: string) {
  const clean = path.trim().replace(/^\/+/, '')
  return `${base}/${clean}`
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  if (!base) throw new Error('Missing VITE_API_BASE_URL')
  const res = await fetch(buildUrl(path), {
    headers: { 'Content-Type': 'application/json', Accept: 'application/json, text/plain, */*' },
    ...init,
  })

  const text = await res.text()
  const asJson = text ? (() => { try { return JSON.parse(text) } catch { return null } })() : null

  if (!res.ok) {
    const detail = typeof asJson === 'object' && asJson && 'message' in asJson ? (asJson as any).message : text
    throw new Error(detail || `HTTP ${res.status}`)
  }

  return (asJson ?? (text as unknown)) as T
}

export type ProductSource = {
  id: string
  productId: string
  store: string
  externalId: string | null
  sourceUrl: string | null
  isPrimary: boolean
}

export type Product = {
  id: string
  title: string
  url: string
  description: string | null
  imagePathInStorage: string | null
  createdAt: string
  updatedAt: string
  sources?: ProductSource[]
}

export type PriceSnapshot = {
  id: string
  productSourceId: string
  price: number
  currency: string
  collectedAt: string
  rawDataJson: string | null
}

export async function createProduct(input: { title: string; url: string; description?: string | null }): Promise<Product> {
  return request<Product>('/api/products', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function addProductSource(
  productId: string,
  input: { store: string; externalId?: string; sourceUrl?: string; isPrimary?: boolean }
): Promise<ProductSource> {
  return request<ProductSource>(`/api/products/${productId}/sources`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function triggerFetch(productId: string): Promise<PriceSnapshot> {
  return request<PriceSnapshot>(`/api/products/${productId}/fetch`, {
    method: 'POST',
  })
}

export async function getPrices(productId: string, take = 50): Promise<PriceSnapshot[]> {
  const search = take ? `?take=${take}` : ''
  return request<PriceSnapshot[]>(`/api/products/${productId}/prices${search}`)
}

export async function seedDemo(): Promise<{ productId: string; sourceId: string; product: Product }> {
  return request('/dev/seed', { method: 'POST' })
}
