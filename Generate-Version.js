const fs = require('fs');
const { execSync } = require('child_process');

const filePath = './version.json';

const raw = fs.readFileSync(filePath, 'utf-8');

// Remove BOM if present
const clean = raw.replace(/^\uFEFF/, '');

// Load existing version
const versionData = JSON.parse(clean)

// --- CONFIG ---
const bumpType = process.argv[2] || 'patch';
// options: patch | minor | major

function bumpVersion(version, type) {
    let [major, minor, patch] = version.split('.').map(Number);

    if (type === 'major') {
        major++;
        minor = 0;
        patch = 0;
    } else if (type === 'minor') {
        minor++;
        patch = 0;
    } else {
        patch++;
    }

    return `${major}.${minor}.${patch}`;
}

// Update version
versionData.version = bumpVersion(versionData.version, bumpType);

// Increment build
versionData.build += 1;

// Get git commit hash
try {
    versionData.commit = execSync('git rev-parse --short HEAD')
        .toString()
        .trim();
} catch {
    versionData.commit = "no-git";
}

// Set date
versionData.date = new Date().toISOString();

// Write file
fs.writeFileSync(filePath, JSON.stringify(versionData, null, 2));

console.log('Updated version.json:', versionData);