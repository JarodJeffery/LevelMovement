import { useBookingController } from '../../controllers/useBookingController';
import { ErrorNotice } from '../../../../shared/components/ErrorNotice';
export function BookingPanel() {
  const controller = useBookingController();
  return <section className="panel booking"><p className="eyebrow">ELIGIBILITY DEMO</p><h2>Can this vehicle be booked?</h2><p>Try an active registration, then delete the vehicle and check again. This check does not create a booking.</p><form onSubmit={event => { event.preventDefault(); controller.check.mutate(); }}><label htmlFor="booking-registration">Registration to check</label><div className="inline"><input id="booking-registration" required maxLength={20} value={controller.registration} onChange={event => { controller.setRegistration(event.target.value); controller.check.reset(); }} placeholder="CA 123-456" /><button disabled={controller.check.isPending || !controller.registration.trim()}>Check eligibility</button></div></form><ErrorNotice error={controller.check.error} />{controller.check.data && <p className="notice success" role="status">{controller.check.data.message}</p>}</section>;
}
