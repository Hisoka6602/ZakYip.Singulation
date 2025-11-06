# Branch Cleanup Workflow

## Overview
This workflow deletes all branches except `master` from the repository.

## How to Use

1. Go to the repository on GitHub
2. Click on "Actions" tab
3. Select "Delete All Branches Except Master" workflow from the left sidebar
4. Click "Run workflow" button
5. Confirm by clicking the green "Run workflow" button

## What It Does

The workflow will:
- Fetch all remote branches
- Identify all branches except `master`
- Delete each branch from the remote repository
- Report any failures during deletion

## Notes

- This action is **irreversible** - deleted branches cannot be recovered unless you have local copies
- The workflow requires `contents: write` permission
- Only branches on the remote repository will be deleted (local branches are not affected)
- The `master` branch is protected and will not be deleted
