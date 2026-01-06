import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getPrices, type PriceSnapshot } from '../lib/api'

export default function ProductHistory() {
  const { id } = useParams()
  const [prices, setPrices] = useState<PriceSnapshot[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>('')

  useEffect(() => {
    if (!id) return
    setLoading(true)
    setError('')
    getPrices(id)
      .then(setPrices)
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load prices'))
      .finally(() => setLoading(false))
  }, [id])

  return (
    <div style={{ maxWidth: 960, margin: '0 auto', padding: 16 }}>
      <h1 style={{ marginBottom: 4 }}>Product History</h1>
      <p style={{ opacity: 0.8, fontFamily: 'monospace' }}>Product ID: {id}</p>

      {loading && <p>Loading…</p>}
      {error && <p style={{ color: '#d33' }}>{error}</p>}

      {prices.length > 0 ? (
        <div style={{ marginTop: 12 }}>
          <h2 style={{ margin: '8px 0' }}>Recent prices</h2>
          <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: 8 }}>
            {prices.map((p) => (
              <li
                key={p.id}
                style={{
                  padding: 12,
                  border: '1px solid #e0e0e0',
                  borderRadius: 8,
                  display: 'flex',
                  flexWrap: 'wrap',
                  gap: 12,
                  alignItems: 'center',
                  fontFamily: 'monospace',
                }}
              >
                <span>{new Date(p.collectedAt).toLocaleString()}</span>
                <span>{p.price} {p.currency}</span>
                <span style={{ opacity: 0.7 }}>source {p.productSourceId}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : (
        !loading && <p style={{ opacity: 0.8 }}>No prices yet. Trigger a fetch from the home page.</p>
      )}
    </div>
  )
}
