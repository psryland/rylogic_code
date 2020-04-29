class Vec2():
	def __init__(self, x: float = 0, y: float = 0):
		self.x = x
		self.y = y
		return

	@staticmethod
	def from_array(arr: [float]):
		return Vec2(arr[0], arr[1])

	def __pos__(self):
		return Vec2(self.x, self.y)

	def __neg__(self):
		return Vec2(-self.x, -self.y)

	def __add__(self, rhs):
		return Vec2(self.x + rhs.x, self.y + rhs.y)

	def __sub__(self, rhs):
		return Vec2(self.x - rhs.x, self.y - rhs.y)

	def __mul__(self, rhs):
		if rhs is float or rhs is int:
			return Vec2(self.x * rhs, self.y * rhs)
		if rhs is Vec2:
			return Vec2(self.x * rhs.x, self.y * rhs.y)

	def __div__(self, rhs):
		if rhs is float or rhs is int:
			return Vec2(self.x / rhs, self.y / rhs)
		if rhs is Vec2:
			return Vec2(self.x / rhs.x, self.y / rhs.y)


	def __str__(self):
		return f"{self.x} {self.y}"

