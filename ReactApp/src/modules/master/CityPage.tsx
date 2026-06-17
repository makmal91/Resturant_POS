import SettingsMasterPage from './SettingsMasterPage';

const CityPage = () => (
  <SettingsMasterPage
    type="city"
    title="Cities"
    subtitle="Manage cities by country for branches and customer addresses."
    entityLabel="City"
    permissionModule="Cities"
  />
);

export default CityPage;
