import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { addProductSource, createProduct, getPrices, seedDemo, triggerFetch, type PriceSnapshot } from '../lib/api'

type Status = { kind: 'ok'; message: string } | { kind: 'error'; message: string } | null

function joinUrl(baseUrl: string, path: string) {
  const base = baseUrl.trim().replace(/\/+$/, '')
  const p = path.trim().replace(/^\/+/, '')
  if (!base) return `/${p}`
  if (!p) return base
  return `${base}/${p}`
}

export default function Home() {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL

  const [productId, setProductId] = useState('')
  const [sourceId, setSourceId] = useState('')
  const [prices, setPrices] = useState<PriceSnapshot[]>([])
  const [latest, setLatest] = useState<PriceSnapshot | null>(null)
  const [loading, setLoading] = useState(false)
  const [status, setStatus] = useState<Status>(null)

  const [createInput, setCreateInput] = useState({
    title: 'Demo Product',
    url: 'https://example.com/demo-product',
    description: 'Demo product for testing fetch',
  })

  const [sourceInput, setSourceInput] = useState({
    store: 'amazon',
    externalId: 'DEMO-ASIN-123',
    sourceUrl: 'https://amazon.com/dp/DEMO-ASIN-123',
    isPrimary: true,
  })

  const envStatus = useMemo(() => {
    const missing: string[] = []
    if (!import.meta.env.VITE_API_BASE_URL) missing.push('VITE_API_BASE_URL')
    return {
      ok: missing.length === 0,
      missing,
    }
  }, [])

  async function handleCreateProduct() {
    setStatus(null)
    setLoading(true)
    try {
      const product = await createProduct(createInput)
      setProductId(product.id)
      setStatus({ kind: 'ok', message: `Product created: ${product.id}` })
    } catch (error) {
      setStatus({ kind: 'error', message: error instanceof Error ? error.message : 'Create failed' })
    } finally {
      setLoading(false)
    }
  }

  async function handleAddSource() {
    setStatus(null)
    if (!productId.trim()) {
      setStatus({ kind: 'error', message: 'Set a product ID first.' })
      return
    }
    setLoading(true)
    try {
      const source = await addProductSource(productId.trim(), sourceInput)
      setSourceId(source.id)
      setStatus({ kind: 'ok', message: `Source added: ${source.id}` })
    } catch (error) {
      setStatus({ kind: 'error', message: error instanceof Error ? error.message : 'Add source failed' })
    } finally {
      setLoading(false)
    }
  }

  async function handleFetch() {
    setStatus(null)
    if (!productId.trim()) {
      setStatus({ kind: 'error', message: 'Set a product ID first.' })
      return
    }
    setLoading(true)
    try {
      const snapshot = await triggerFetch(productId.trim())
      setLatest(snapshot)
      setStatus({ kind: 'ok', message: `Fetched price: ${snapshot.price} ${snapshot.currency}` })
    } catch (error) {
      setStatus({ kind: 'error', message: error instanceof Error ? error.message : 'Fetch failed' })
    } finally {
      setLoading(false)
    }
  }

  async function handleLoadPrices() {
    setStatus(null)
    if (!productId.trim()) {
      setStatus({ kind: 'error', message: 'Set a product ID first.' })
      return
    }
    setLoading(true)
    try {
      const list = await getPrices(productId.trim())
      setPrices(list)
      setStatus({ kind: 'ok', message: `Loaded ${list.length} prices` })
    } catch (error) {
      setStatus({ kind: 'error', message: error instanceof Error ? error.message : 'Load prices failed' })
    } finally {
      setLoading(false)
    }
  }

  async function handleSeedDemo() {
    setStatus(null)
    setLoading(true)
    try {
      const res = await seedDemo()
      setProductId(res.productId)
      setSourceId(res.sourceId)
      setStatus({ kind: 'ok', message: `Seeded product ${res.productId}` })
    } catch (error) {
      setStatus({ kind: 'error', message: error instanceof Error ? error.message : 'Seed failed' })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{ textAlign: 'left', width: 'min(980px, 100%)' }}>
      <header style={{ display: 'flex', alignItems: 'baseline', gap: 16 }}>
        <h1 style={{ margin: 0 }}>TagTrack</h1>
        <span style={{ opacity: 0.8 }}>Backend integration</span>
      </header>

      <div style={{ marginTop: 12, opacity: 0.85 }}>
        <div>
          API: <span style={{ fontFamily: 'monospace' }}>{apiBaseUrl || '(missing)'}</span>
        </div>
        {!envStatus.ok && (
          <div style={{ marginTop: 8 }}>
            Missing env: <span style={{ fontFamily: 'monospace' }}>{envStatus.missing.join(', ')}</span>
          </div>
        )}
      </div>

      <div style={{ display: 'flex', gap: 8, marginTop: 16, alignItems: 'center' }}>
        <button type="button" onClick={handleSeedDemo} disabled={loading}>
          Seed demo product
        </button>
        <span style={{ opacity: 0.8, fontFamily: 'monospace' }}>
          Join: {joinUrl(apiBaseUrl ?? '', '/dev/seed')}
        </span>
        <div style={{ flex: 1 }} />
        <Link to="/product/example" style={{ alignSelf: 'center' }}>
          Go to product page
        </Link>
      </div>

      <section style={{ marginTop: 16, display: 'grid', gap: 16 }}>
        <div style={{ display: 'grid', gap: 8 }}>
          <h2 style={{ margin: '8px 0' }}>1) Create product</h2>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>Title</span>
            <input value={createInput.title} onChange={(e) => setCreateInput({ ...createInput, title: e.target.value })} />
          </label>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>URL</span>
            <input value={createInput.url} onChange={(e) => setCreateInput({ ...createInput, url: e.target.value })} />
          </label>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>Description</span>
            <textarea
              value={createInput.description}
              onChange={(e) => setCreateInput({ ...createInput, description: e.target.value })}
              rows={3}
            />
          </label>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <button type="button" onClick={handleCreateProduct} disabled={loading}>
              {loading ? 'Working…' : 'Create product'}
            </button>
            {productId && <span style={{ fontFamily: 'monospace' }}>productId: {productId}</span>}
          </div>
        </div>

        <div style={{ display: 'grid', gap: 8 }}>
          <h2 style={{ margin: '8px 0' }}>2) Add source</h2>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>Product ID</span>
            <input value={productId} onChange={(e) => setProductId(e.target.value)} placeholder="Set from step 1 or seed" />
          </label>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>Store</span>
            <input value={sourceInput.store} onChange={(e) => setSourceInput({ ...sourceInput, store: e.target.value })} />
          </label>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>External ID</span>
            <input
              value={sourceInput.externalId}
              onChange={(e) => setSourceInput({ ...sourceInput, externalId: e.target.value })}
              placeholder="e.g. ASIN"
            />
          </label>
          <label style={{ display: 'grid', gap: 6 }}>
            <span>Source URL</span>
            <input
              value={sourceInput.sourceUrl}
              onChange={(e) => setSourceInput({ ...sourceInput, sourceUrl: e.target.value })}
              placeholder="product URL at store"
            />
          </label>
          <label style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <input
              type="checkbox"
              checked={sourceInput.isPrimary}
              onChange={(e) => setSourceInput({ ...sourceInput, isPrimary: e.target.checked })}
            />
            <span>Primary source</span>
          </label>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <button type="button" onClick={handleAddSource} disabled={loading}>
              {loading ? 'Working…' : 'Add source'}
            </button>
            {sourceId && <span style={{ fontFamily: 'monospace' }}>sourceId: {sourceId}</span>}
          </div>
        </div>

        <div style={{ display: 'grid', gap: 8 }}>
          <h2 style={{ margin: '8px 0' }}>3) Fetch price & history</h2>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <button type="button" onClick={handleFetch} disabled={loading}>
              {loading ? 'Working…' : 'Trigger fetch'}
            </button>
            <button type="button" onClick={handleLoadPrices} disabled={loading}>
              {loading ? 'Working…' : 'Load prices'}
            </button>
            <Link to={`/product/${productId || 'example'}`} style={{ marginLeft: 'auto' }}>
              View history page
            </Link>
          </div>
          {latest && (
            <div style={{ padding: 12, border: '1px solid #ddd', borderRadius: 8 }}>
              <div style={{ fontWeight: 600 }}>Latest fetch</div>
              <div style={{ fontFamily: 'monospace' }}>
                {latest.price} {latest.currency} at {new Date(latest.collectedAt).toLocaleString()}
              </div>
            </div>
          )}
          {prices.length > 0 && (
            <div style={{ display: 'grid', gap: 4 }}>
              <div style={{ fontWeight: 600 }}>Recent prices</div>
              <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                {prices.map((p) => (
                  <li key={p.id} style={{ display: 'flex', gap: 8, fontFamily: 'monospace' }}>
                    <span>{new Date(p.collectedAt).toLocaleString()}</span>
                    <span>{p.price} {p.currency}</span>
                    <span style={{ opacity: 0.7 }}>source {p.productSourceId}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      </section>

      {status && (
        <div
          style={{
            marginTop: 16,
            padding: 12,
            borderRadius: 8,
            border: '1px solid',
            borderColor: status.kind === 'ok' ? '#2ecc71' : '#e74c3c',
            background: status.kind === 'ok' ? '#eafaf1' : '#fdecea',
          }}
        >
          {status.message}
        </div>
      )}
    </div>
  )
}