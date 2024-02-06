set -e

mongosh <<EOF
db = db.getSiblingDB('sales')

db.createUser({
  user: 'sales',
  pwd: 'sales',
  roles: [{ role: 'readWrite', db: 'sales' }],
});
db.createCollection('Pagamento')

EOF