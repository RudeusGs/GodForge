import { writeFileSync, chmodSync } from 'node:fs';
import { join } from 'node:path';
import { command } from './m2-environment.mjs';

// An anonymous read-only HTTPS mirror of the disposable provider repository.
// The CA is trusted only by this test worker; production TLS/DNS policy is intact.
export function configureLinkedFixture(directory) {
  process.env.GODFORGE_M2_TLS_DIR = directory.replaceAll('\\', '/');
  const openssl = process.platform === 'win32' ? 'C:/Program Files/Git/usr/bin/openssl.exe' : 'openssl';
  command(openssl, ['req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '1',
    '-keyout', join(directory, 'key.pem'), '-out', join(directory, 'ca.pem'),
    '-subj', '/CN=linked-tls', '-addext', 'subjectAltName=DNS:linked-tls']);
  chmodSync(directory, 0o755); chmodSync(join(directory, 'ca.pem'), 0o644); chmodSync(join(directory, 'key.pem'), 0o600);
  const authorization = Buffer.from(`forgejo-viewer:${process.env.GODFORGE_TEST_FORGEJO_VIEWER_TOKEN}`).toString('base64');
  writeFileSync(join(directory, 'nginx.conf'), `events {}\nhttp { server {
    listen 3443 ssl; ssl_certificate /fixture/ca.pem; ssl_certificate_key /fixture/key.pem;
    location ~ "^/forgejo-service/godot-fixture-[a-f0-9]{32}\\.git/(info/refs|git-upload-pack)$" {
      if ($args ~ "git-receive-pack") { return 403; }
      proxy_set_header Authorization "Basic ${authorization}";
      proxy_pass http://forgejo:3000;
    }
    location / { return 403; }
  } }`);
  chmodSync(join(directory, 'nginx.conf'), 0o600);
}
