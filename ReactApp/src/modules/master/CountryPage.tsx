import SettingsMasterPage from './SettingsMasterPage';

const CountryPage = () => (
  <SettingsMasterPage
    type="country"
    title="Countries"
    subtitle="Manage countries used in branches and customer addresses."
    entityLabel="Country"
    permissionModule="Countries"
  />
);

export default CountryPage;
