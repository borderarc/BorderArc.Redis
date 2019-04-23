#!/bin/sh
# Generate the docs only if master, the travis_build_docs is true and we can use secure variables
  #if [[ "$TRAVIS_BRANCH" = "master" && -n "$TRAVIS_BUILD_DOCS" && "$TRAVIS_PULL_REQUEST" = "false" ]] ; then
  #    cd $PROJECT_DIR_ABS
  #    source .ci/setup-ssh.sh || travis_terminate 1
  #    .ci/generateDocumentation.sh || travis_terminate 1
  #  fi
$TRAVIS_BRANCH = $1
$TRAVIS_BUILD_DOCS = $2
$PROJECT_DIR_ABS = $3
echo "args [$TRAVIS_BRANCH" = "master" && -n "$TRAVIS_BUILD_DOCS]";
if [[ "$TRAVIS_BRANCH" = "master" && -n "$TRAVIS_BUILD_DOCS" ]] ; then
    cd $PROJECT_DIR_ABS
    source .ci/setup-ssh.sh || travis_terminate 1
    .ci/generateDocumentation.sh || travis_terminate 1
fi