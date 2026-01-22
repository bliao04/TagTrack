import { Link } from 'react-router-dom'

export default function NavBar() {
	return (
		<header style={{ borderBottom: '1px solid #e2e8f0', background: '#ffffffc7', backdropFilter: 'blur(6px)' }}>
			<div style={{ width: 'min(1100px, 92vw)', margin: '0 auto', display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '14px 0' }}>
				<Link to="/" style={{ display: 'flex', alignItems: 'center', gap: 10, fontWeight: 800, color: '#0f172a' }}>
					<span style={{ width: 34, height: 34, borderRadius: 10, background: 'linear-gradient(140deg, #2563eb, #0ea5e9)', display: 'inline-flex', alignItems: 'center', justifyContent: 'center', color: '#fff' }}>T</span>
					<span>TagTrack</span>
				</Link>
				<nav style={{ display: 'flex', gap: 18, alignItems: 'center', fontWeight: 600, color: '#475569' }}>
					<Link to="/">Home</Link>
					<Link to="/product/example">Products</Link>
					<a href="https://github.com/bliao04/TagTrack" target="_blank" rel="noreferrer">GitHub</a>
				</nav>
			</div>
		</header>
	)
}
