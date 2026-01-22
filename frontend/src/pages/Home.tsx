import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import NavBar from '../components/NavBar'
import { listProducts, searchProducts } from '../lib/api'

type SearchResult = {
  id: string
  title: string
  sources: number
  latestPrice?: string
  updatedAt?: string
}

const sampleResults: SearchResult[] = []

export default function Home() {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState<SearchResult[]>(sampleResults)
  const [searching, setSearching] = useState(false)
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    let cancelled = false
    async function load() {
      setLoading(true)
      try {
        const products = await listProducts()
        if (cancelled) return
        const mapped: SearchResult[] = products.map((p) => ({
          id: p.id,
          title: p.title,
          sources: p.sources?.length ?? 0,
          latestPrice: p.latestPrice ? `${p.latestPrice.price} ${p.latestPrice.currency}` : undefined,
          updatedAt: p.latestPrice ? new Date(p.latestPrice.collectedAt).toLocaleString() : new Date(p.updatedAt).toLocaleDateString(),
        }))
        setResults(mapped.length ? mapped : sampleResults)
        setMessage(mapped.length ? '' : 'No products yet—seed samples or add your own.')
      } catch (error) {
        if (!cancelled) setMessage(error instanceof Error ? error.message : 'Failed to load products')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    load()
    return () => {
      cancelled = true
    }
  }, [])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return results
    return results.filter((r) => r.title.toLowerCase().includes(q))
  }, [query, results])

  async function handleSearch(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setSearching(true)
    setMessage('')
    try {
      const res = await searchProducts(query, 1, 20)
      const mapped: SearchResult[] = res.items.map((p) => ({
        id: p.id,
        title: p.title,
        sources: p.sources?.length ?? 0,
        latestPrice: p.latestPrice ? `${p.latestPrice.price} ${p.latestPrice.currency}` : undefined,
        updatedAt: p.latestPrice ? new Date(p.latestPrice.collectedAt).toLocaleString() : new Date(p.updatedAt).toLocaleDateString(),
      }))
      setResults(mapped)
      if (mapped.length === 0) setMessage('No results. Try another query or add a product by URL.')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Search failed')
    } finally {
      setSearching(false)
    }
  }

  return (
    <div className="app-shell">
      <NavBar />

      <main className="page-width">
        <section className="hero">
          <h1 className="hero-title">Track prices across sources with one search</h1>
          <p className="hero-subtitle">
            Discover products, connect their sources, and fetch live prices without manual data entry. Start by searching our catalog or add a new product URL to track.
          </p>
          <div className="search-card">
            <form className="search-bar" onSubmit={handleSearch}>
              <input
                className="search-input"
                placeholder="Search products or paste an Amazon URL"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
              />
              <button className="search-button" type="submit" disabled={searching}>
                {searching ? 'Searching…' : 'Search'}
              </button>
            </form>
            {message && <div style={{ marginTop: 10, color: '#475569' }}>{message}</div>}
          </div>
        </section>

        <section>
          <h3 className="section-title">Results</h3>
          <div className="results-grid">
            {filtered.map((item) => (
              <article key={item.id} className="result-card">
                <div className="pill">{item.sources} sources</div>
                <div style={{ display: 'grid', gap: 4 }}>
                  <p className="result-title">{item.title}</p>
                  <div className="result-meta">{item.latestPrice || 'Price pending'} · {item.updatedAt || 'Awaiting fetch'}</div>
                </div>
                <Link to={`/product/${item.id}`} style={{ fontWeight: 700, color: '#2563eb' }}>
                  View
                </Link>
              </article>
            ))}
          </div>
        </section>

        <section>
          <div className="cta-card">
            <div>
              <div style={{ fontWeight: 800, marginBottom: 6 }}>Can’t find it? Add a URL.</div>
              <div style={{ opacity: 0.9 }}>Paste any product link to create a tracked item and connect its sources.</div>
            </div>
            <Link to="/product/example" className="cta-button">
              Add product
            </Link>
          </div>
        </section>
      </main>
    </div>
  )
}