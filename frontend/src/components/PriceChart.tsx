import type { PriceSnapshot } from '../lib/api'

type Props = {
	prices: PriceSnapshot[]
}

export default function PriceChart({ prices }: Props) {
	if (!prices.length) return <div style={{ padding: 12, color: '#64748b' }}>No price history yet.</div>

	const sorted = [...prices].sort((a, b) => new Date(a.collectedAt).getTime() - new Date(b.collectedAt).getTime())
	const values = sorted.map((p) => p.price)
	const min = Math.min(...values)
	const max = Math.max(...values)
	const range = max - min || 1

	const points = sorted.map((p, idx) => {
		const x = (idx / Math.max(1, sorted.length - 1)) * 100
		const y = 100 - ((p.price - min) / range) * 100
		return `${x},${y}`
	})

	const formatCurrency = sorted[0].currency

	return (
		<div style={{ padding: 12 }}>
			<div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, color: '#0f172a' }}>
				<div style={{ fontWeight: 700 }}>Price history</div>
				<div style={{ fontFamily: 'monospace', color: '#475569' }}>
					Min {min.toFixed(2)} {formatCurrency} · Max {max.toFixed(2)} {formatCurrency}
				</div>
			</div>
			<svg viewBox="0 0 100 100" role="img" aria-label="Price history chart" style={{ width: '100%', height: 160, background: '#f8fafc', borderRadius: 10 }}>
				<polyline
					fill="none"
					stroke="#2563eb"
					strokeWidth={2.4}
					strokeLinecap="round"
					points={points.join(' ')}
				/>
			</svg>
		</div>
	)
}
