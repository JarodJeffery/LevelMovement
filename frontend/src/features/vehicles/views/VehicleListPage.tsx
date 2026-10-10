import { Link, useLocation } from 'react-router-dom';
import { useVehicleListController } from '../controllers/useVehicleListController';
import { ErrorNotice } from '../../../shared/components/ErrorNotice';
import { BookingPanel } from './components/BookingPanel';
export function VehicleListPage() {
  const list = useVehicleListController();
  const location = useLocation();
  return <><div className="page-heading"><div><p className="eyebrow">YOUR FLEET</p><h1>Vehicles</h1><p>Manage registrations, daily rates and availability.</p></div><Link className="button" to="/vehicles/new">Register vehicle</Link></div>{location.state?.message && <p role="status" className="notice success">{location.state.message}</p>}<section className="panel"><div className="section-heading"><h2>Active vehicles</h2><button className="secondary" onClick={() => void list.refetch()} disabled={list.isFetching}>Refresh</button></div><ErrorNotice error={list.error} />{list.isPending ? <p role="status">Loading vehicles…</p> : list.data?.length === 0 ? <div className="empty"><h3>Your fleet starts here</h3><p>Register your first vehicle to see it in this list.</p></div> : list.data && <div className="table-scroll"><table><thead><tr><th>Registration</th><th>Vehicle</th><th>Year</th><th>Daily rate</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>{list.data.map(vehicle => <tr key={vehicle.registrationNumber}><td><strong>{vehicle.registrationNumber}</strong></td><td>{vehicle.make} {vehicle.model}</td><td>{vehicle.year}</td><td>R {vehicle.dailyRate.toFixed(2)}</td><td><Link to={`/vehicles/${encodeURIComponent(vehicle.registrationNumber)}`}>View / edit<span className="sr-only"> {vehicle.registrationNumber}</span></Link></td></tr>)}</tbody></table></div>}</section><BookingPanel /></>;
}
