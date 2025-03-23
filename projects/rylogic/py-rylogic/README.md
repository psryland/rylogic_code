# Python Bindings for Rylogic Code

Library of Rylogic Code related types

## How to Build

Ensure `build` is up to date

```python -m pip install --upgrade build```

Build from the root directory using

```python -m build```

## How to Test

Ensure pytest is installed

  ```pip install pytest```

Run `pytest` from the project root

Might need to run this first

```pip install -e .```

Individual files can be tested using `pytest tests/test_vector.py`

## How to Publish

Ensure `twine` is installed

```python -m pip install --upgrade twine```

Upload to PyPI

```python -m twine upload dist/*```

You'll need your account user/pass

