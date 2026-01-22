import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import NavBar from '../components/NavBar'
import PriceChart from '../components/PriceChart'
import { getPrices, getProduct, type PriceSnapshot, type ProductDetail } from '../lib/api'

export default function ProductHistory() {
  const { id } = useParams()
  const [product, setProduct] = useState<ProductDetail | null>(null)
  const [prices, setPrices] = useState<PriceSnapshot[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string>('')

  useEffect(() => {
    if (!id) return
    setLoading(true)
    setError('')
    Promise.all([getProduct(id), getPrices(id, 200)])
      .then(([prod, priceList]) => {
        setProduct(prod)
        setPrices(priceList)
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load product'))
      .finally(() => setLoading(false))
  }, [id])

  const latest = prices.at(0)

  return (
    <div className="app-shell">
      <NavBar />
      <main className="page-width" style={{ padding: '32px 0', display: 'grid', gap: 20 }}>
        {loading && <p>Loading…</p>}
        {error && <p style={{ color: '#d33' }}>{error}</p>}

        {product && (
          <section className="product-hero">
            <div className="product-image" aria-label="Product image placeholder">
              {product.imagePathInStorage ? (
                <img src={product.imagePathInStorage} alt={product.title} style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: 12 }} />
              ) : (
                <div className="product-image-fallback">Image coming soon</div>
              )}
            </div>
            <div className="product-meta">
              <div className="pill">{product.sources?.length ?? 0} sources</div>
              <h1 className="product-title">{product.title}</h1>
              <div className="product-desc">{product.description || 'No description yet.'}</div>
              <div className="product-latest">
                <div>
                  <div className="product-latest-label">Latest price</div>
                  <div className="product-latest-value">
                    {latest ? (
                      <>
                        {latest.price} {latest.currency}
                        <span className="product-latest-date"> · {new Date(latest.collectedAt).toLocaleString()}</span>
                      </>
                    ) : (
                      'Price pending'
                    )}
                  </div>
                </div>
                <Link to={product.url} target="_blank" rel="noreferrer" className="cta-button" style={{ textDecoration: 'none' }}>
                  View source
                </Link>
              </div>
              <div className="product-sources">
                {(product.sources ?? []).map((s) => (
                  <div key={s.id} className="product-source-card">
                    <div className="product-source-title">{s.store}</div>
                    <div className="product-source-meta">{s.externalId || s.sourceUrl || 'No id'}</div>
                    {s.isPrimary && <span className="pill" style={{ background: '#dcfce7', color: '#166534' }}>Primary</span>}
                  </div>
                ))}
              </div>
            </div>
          </section>
        )}

        <section className="product-chart">
          <PriceChart prices={prices} />
        </section>

        <section>
          <h3 className="section-title">Price history</h3>
          {prices.length > 0 ? (
            <ul className="price-list">
              {prices.map((p) => (
                <li key={p.id} className="price-row">
                  <span>{new Date(p.collectedAt).toLocaleString()}</span>
                  <span>{p.price} {p.currency}</span>
                  <span className="price-source">source {p.productSourceId}</span>
                </li>
              ))}
            </ul>
          ) : (
            !loading && <p style={{ color: '#64748b' }}>No prices yet. Trigger a fetch from the home page.</p>
          )}
        </section>
      </main>
    </div>
  )
}
