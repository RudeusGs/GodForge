import { startProvider, stopEnvironment, command } from './m2-environment.mjs';
let owned = false;
try {
  // startProvider refuses an existing project. Cleanup ownership starts only
  // once the caller has confirmed that the namespace was initially empty.
  await startProvider(); owned = true;
  command('dotnet', ['test', 'GodForge-BE/tests/GodForge.IntegrationTests', '--filter', 'FullyQualifiedName~RealForgejoTests', '--logger', 'console;verbosity=minimal'], { stdio: 'inherit' });
} catch (error) { console.error(error.message); process.exitCode = 1; }
finally { stopEnvironment(); }
