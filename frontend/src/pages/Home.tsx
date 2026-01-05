import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { supabase } from '../lib/supabase'

type JsonValue = null | boolean | number | string | JsonValue[] | { [key: string]: JsonValue }

function safeParseJson(input: string):
  | { ok: true; value: JsonValue }
  | { ok: false; error: string } {
  const trimmed = input.trim()
  if (!trimmed) return { ok: false, error: 'Empty JSON' }
  try {
    return { ok: true, value: JSON.parse(trimmed) as JsonValue }
  } catch (error) {
    return {
      ok: false,
      error: error instanceof Error ? error.message : 'Invalid JSON',
    }
  }
}

function joinUrl(baseUrl: string, path: string) {
  const base = baseUrl.trim().replace(/\/+$/, '')
  const p = path.trim().replace(/^\/+/, '')
  if (!base) return `/${p}`
  if (!p) return base
  return `${base}/${p}`
}

export default function Home() {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL
  const supabaseUrl = import.meta.env.VITE_SUPABASE_URL

  const [activeTab, setActiveTab] = useState<'supabase' | 'backend'>('supabase')

  // --- Supabase test state
  const [sbTable, setSbTable] = useState('')
  const [sbSelect, setSbSelect] = useState('*')
  const [sbLimit, setSbLimit] = useState(10)
  const [sbEqColumn, setSbEqColumn] = useState('')
  const [sbEqValue, setSbEqValue] = useState('')
  const [sbInsertJson, setSbInsertJson] = useState('{\n  \n}')
  const [sbLoading, setSbLoading] = useState(false)
  const [sbResult, setSbResult] = useState<string>('')
  const [sbError, setSbError] = useState<string>('')

  // --- Backend test state
  const [bePath, setBePath] = useState('/openapi/v1.json')
  const [beMethod, setBeMethod] = useState<'GET' | 'POST'>('GET')
  const [beBodyJson, setBeBodyJson] = useState('{\n  \n}')
  const [beLoading, setBeLoading] = useState(false)
  const [beResult, setBeResult] = useState<string>('')
  const [beError, setBeError] = useState<string>('')

  const envStatus = useMemo(() => {
    const missing: string[] = []
    if (!import.meta.env.VITE_SUPABASE_URL) missing.push('VITE_SUPABASE_URL')
    if (!import.meta.env.VITE_SUPABASE_ANON_KEY) missing.push('VITE_SUPABASE_ANON_KEY')
    if (!import.meta.env.VITE_API_BASE_URL) missing.push('VITE_API_BASE_URL')
    return {
      ok: missing.length === 0,
      missing,
    }
  }, [])

  async function runSupabaseSelect() {
    setSbError('')
    setSbResult('')
    const table = sbTable.trim()
    if (!table) {
      setSbError('Enter a table name to query (e.g. products).')
      return
    }

    setSbLoading(true)
    try {
      let query = supabase.from(table).select(sbSelect.trim() || '*')

      const eqColumn = sbEqColumn.trim()
      if (eqColumn) query = query.eq(eqColumn, sbEqValue)

      const limit = Number.isFinite(sbLimit) ? Math.max(1, Math.min(1000, sbLimit)) : 10
      query = query.limit(limit)

      const { data, error } = await query
      if (error) throw error

      setSbResult(JSON.stringify({ table, select: sbSelect, limit, data }, null, 2))
    } catch (error) {
      setSbError(error instanceof Error ? error.message : 'Supabase request failed')
    } finally {
      setSbLoading(false)
    }
  }

  async function runSupabaseInsert() {
    setSbError('')
    setSbResult('')
    const table = sbTable.trim()
    if (!table) {
      setSbError('Enter a table name to insert into (e.g. products).')
      return
    }

    const parsed = safeParseJson(sbInsertJson)
    if (!parsed.ok) {
      setSbError(`Insert JSON error: ${parsed.error}`)
      return
    }

    if (parsed.value === null || typeof parsed.value !== 'object') {
      setSbError('Insert JSON must be an object or an array of objects.')
      return
    }

    setSbLoading(true)
    try {
      const { data, error } = await supabase
        .from(table)
        // Supabase accepts object or array of objects
        .insert(parsed.value as unknown)
        .select()

      if (error) throw error
      setSbResult(JSON.stringify({ table, inserted: data }, null, 2))
    } catch (error) {
      setSbError(error instanceof Error ? error.message : 'Supabase insert failed')
    } finally {
      setSbLoading(false)
    }
  }

  async function runBackendRequest() {
    setBeError('')
    setBeResult('')
    const url = joinUrl(apiBaseUrl ?? '', bePath)
    if (!apiBaseUrl) {
      setBeError('Missing VITE_API_BASE_URL (set it in a .env file for Vite).')
      return
    }

    setBeLoading(true)
    try {
      const init: RequestInit = {
        method: beMethod,
        headers: {
          Accept: 'application/json, text/plain, */*',
        },
      }

      if (beMethod === 'POST') {
        const parsed = safeParseJson(beBodyJson)
        if (!parsed.ok) {
          setBeError(`Body JSON error: ${parsed.error}`)
          return
        }
        init.headers = { ...init.headers, 'Content-Type': 'application/json' }
        init.body = JSON.stringify(parsed.value)
      }

      const res = await fetch(url, init)
      const contentType = res.headers.get('content-type') ?? ''
      const raw = await res.text()
      const maybeJson = contentType.includes('application/json')
        ? safeParseJson(raw)
        : null

      const payload = {
        url,
        status: res.status,
        ok: res.ok,
        contentType,
        body: maybeJson && maybeJson.ok ? maybeJson.value : raw,
      }
      setBeResult(JSON.stringify(payload, null, 2))

      if (!res.ok) {
        setBeError(`HTTP ${res.status} ${res.statusText}`)
      }
    } catch (error) {
      setBeError(error instanceof Error ? error.message : 'Backend request failed')
    } finally {
      setBeLoading(false)
    }
  }

  return (
    <div style={{ textAlign: 'left', width: 'min(980px, 100%)' }}>
      <header style={{ display: 'flex', alignItems: 'baseline', gap: 16 }}>
        <h1 style={{ margin: 0 }}>TagTrack</h1>
        <span style={{ opacity: 0.8 }}>Prototype integration panel</span>
      </header>

      <div style={{ marginTop: 12, opacity: 0.85 }}>
        <div>
          Supabase: <span style={{ fontFamily: 'monospace' }}>{supabaseUrl || '(missing)'}</span>
        </div>
        <div>
          API: <span style={{ fontFamily: 'monospace' }}>{apiBaseUrl || '(missing)'}</span>
        </div>
        {!envStatus.ok && (
          <div style={{ marginTop: 8 }}>
            Missing env: <span style={{ fontFamily: 'monospace' }}>{envStatus.missing.join(', ')}</span>
          </div>
        )}
      </div>

      <div style={{ display: 'flex', gap: 8, marginTop: 16 }}>
        <button type="button" onClick={() => setActiveTab('supabase')} disabled={activeTab === 'supabase'}>
          Supabase
        </button>
        <button type="button" onClick={() => setActiveTab('backend')} disabled={activeTab === 'backend'}>
          Backend
        </button>
        <div style={{ flex: 1 }} />
        <Link to="/product/example" style={{ alignSelf: 'center' }}>
          Go to product page
        </Link>
      </div>

      {activeTab === 'supabase' ? (
        <section style={{ marginTop: 16 }}>
          <h2 style={{ margin: '8px 0' }}>Supabase quick test</h2>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <label style={{ display: 'grid', gap: 6 }}>
              <span>Table</span>
              <input value={sbTable} onChange={(e) => setSbTable(e.target.value)} placeholder="e.g. products" />
            </label>

            <label style={{ display: 'grid', gap: 6 }}>
              <span>Select</span>
              <input value={sbSelect} onChange={(e) => setSbSelect(e.target.value)} placeholder="* or id,name" />
            </label>

            <label style={{ display: 'grid', gap: 6 }}>
              <span>Limit (1-1000)</span>
              <input
                type="number"
                value={sbLimit}
                onChange={(e) => setSbLimit(Number(e.target.value))}
                min={1}
                max={1000}
              />
            </label>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
              <label style={{ display: 'grid', gap: 6 }}>
                <span>EQ column (optional)</span>
                <input value={sbEqColumn} onChange={(e) => setSbEqColumn(e.target.value)} placeholder="e.g. id" />
              </label>
              <label style={{ display: 'grid', gap: 6 }}>
                <span>EQ value</span>
                <input value={sbEqValue} onChange={(e) => setSbEqValue(e.target.value)} placeholder="e.g. 123" />
              </label>
            </div>
          </div>

          <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
            <button type="button" onClick={runSupabaseSelect} disabled={sbLoading}>
              {sbLoading ? 'Working…' : 'Run select'}
            </button>
            <button type="button" onClick={runSupabaseInsert} disabled={sbLoading}>
              {sbLoading ? 'Working…' : 'Insert JSON'}
            </button>
          </div>

          <div style={{ marginTop: 12, display: 'grid', gap: 8 }}>
            <label style={{ display: 'grid', gap: 6 }}>
              <span>Insert payload (object or array)</span>
              <textarea
                value={sbInsertJson}
                onChange={(e) => setSbInsertJson(e.target.value)}
                rows={8}
                style={{ width: '100%' }}
              />
            </label>
          </div>

          {sbError && (
            <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap' }}>Error: {sbError}</pre>
          )}
          {sbResult && (
            <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap' }}>{sbResult}</pre>
          )}
        </section>
      ) : (
        <section style={{ marginTop: 16 }}>
          <h2 style={{ margin: '8px 0' }}>Backend quick test</h2>

          <div style={{ display: 'grid', gridTemplateColumns: '160px 1fr', gap: 12 }}>
            <label style={{ display: 'grid', gap: 6 }}>
              <span>Method</span>
              <select value={beMethod} onChange={(e) => setBeMethod(e.target.value as 'GET' | 'POST')}>
                <option value="GET">GET</option>
                <option value="POST">POST</option>
              </select>
            </label>

            <label style={{ display: 'grid', gap: 6 }}>
              <span>Path</span>
              <input value={bePath} onChange={(e) => setBePath(e.target.value)} placeholder="/health or /api/..." />
            </label>
          </div>

          {beMethod === 'POST' && (
            <label style={{ display: 'grid', gap: 6, marginTop: 12 }}>
              <span>JSON body</span>
              <textarea
                value={beBodyJson}
                onChange={(e) => setBeBodyJson(e.target.value)}
                rows={8}
                style={{ width: '100%' }}
              />
            </label>
          )}

          <div style={{ display: 'flex', gap: 8, marginTop: 12, alignItems: 'center' }}>
            <button type="button" onClick={runBackendRequest} disabled={beLoading}>
              {beLoading ? 'Working…' : 'Send request'}
            </button>
            <span style={{ opacity: 0.8, fontFamily: 'monospace' }}>URL: {joinUrl(apiBaseUrl ?? '', bePath)}</span>
          </div>

          {beError && (
            <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap' }}>Error: {beError}</pre>
          )}
          {beResult && (
            <pre style={{ marginTop: 12, whiteSpace: 'pre-wrap' }}>{beResult}</pre>
          )}
        </section>
      )}
    </div>
  )
}