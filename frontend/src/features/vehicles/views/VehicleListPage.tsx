import { formatRegistration } from '../models/registrationNumber';
import { Link, useLocation } from 'react-router-dom';
import { useVehicleListController } from '../controllers/useVehicleListController';
import { ErrorNotice } from '../../../shared/components/ErrorNotice';
import { BookingPanel } from './components/BookingPanel';

export function VehicleListPage() {
  const list = useVehicleListController();
  const location = useLocation();
  const returnState = { returnTo: location.pathname + location.search };
  const page = list.data;
  const first = page?.count ? (page.pageNumber - 1) * list.pageSize + 1 : 0;
  const last = first ? first + page!.items.length - 1 : 0;
  return <>
    <div className="page-heading"><div><p className="eyebrow">YOUR FLEET</p><h1>Vehicles</h1><p>Manage registrations, daily rates and availability.</p></div>
      <Link className="button" to="/vehicles/new" state={returnState}>Register vehicle</Link></div>
    {location.state?.message && <p role="status" className="notice success">{location.state.message}</p>}
    <section className="panel">
      <div className="section-heading"><h2>Active vehicles</h2><button className="secondary" onClick={() => void list.refetch()} disabled={list.isFetching}>Refresh</button></div>
      <ErrorNotice error={list.error} />
      {list.isPending ? <p role="status">Loading vehicles…</p> : page?.totalPages === 0 ?
        <div className="empty"><h3>Your fleet starts here</h3><p>Register your first vehicle to see it in this list.</p></div> : page &&
        <div className="table-scroll"><table><thead><tr><th>Registration</th><th>Vehicle</th><th>Year</th><th>Daily rate</th><th><span className="sr-only">Actions</span></th></tr></thead>
          <tbody>{page.items.map(vehicle => <tr key={vehicle.registrationNumber}>
            <td><strong>{formatRegistration(vehicle.registrationNumber)}</strong></td><td>{vehicle.make} {vehicle.model}</td><td>{vehicle.year}</td><td>R {vehicle.dailyRate.toFixed(2)}</td>
            <td><Link aria-label={`View / edit ${formatRegistration(vehicle.registrationNumber)}`} to={`/vehicles/${encodeURIComponent(vehicle.registrationNumber)}`} state={returnState}>View / edit<span className="sr-only"> {formatRegistration(vehicle.registrationNumber)}</span></Link></td>
          </tr>)}</tbody></table></div>}
      {page && <nav className="pagination" aria-label="Vehicle pages">
        <p aria-live="polite"><span>Showing {first}–{last}</span><small>{page.count} {page.count === 1 ? 'vehicle' : 'vehicles'} on this page</small></p>
        <div className="page-size"><label htmlFor="page-size">Rows per page</label><select id="page-size" value={list.pageSize} onChange={event => list.setPageSize(Number(event.target.value))}>
          {![10, 25, 50].includes(list.pageSize) && <option value={list.pageSize}>{list.pageSize}</option>}
          <option value="10">10</option><option value="25">25</option><option value="50">50</option>
        </select></div>
        <div className="page-buttons">
          <button className="secondary" disabled={list.isFetching || list.pageNumber <= 1} onClick={() => list.setPage(list.pageNumber - 1)}>Previous</button>
          <span>Page {page.totalPages === 0 ? 0 : page.pageNumber} of {page.totalPages}</span>
          <button className="secondary" disabled={list.isFetching || list.pageNumber >= list.totalPages} onClick={() => list.setPage(list.pageNumber + 1)}>Next</button>
        </div>
      </nav>}
    </section><BookingPanel />
  </>;
}
