import { useParams } from 'react-router-dom'

export default function ProductHistory() {
  const { id } = useParams()
  return (
    <div>
      <h1>Product History</h1>
      <p>Product ID: {id}</p>
    </div>
  )
}
