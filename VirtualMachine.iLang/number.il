Max : [a, b] => {
	if (a > b) then {
		return a;
	} else {
		return b;
	}
}; 

Min : [a, b] => {
	if (a < b) then {
		return a;
	} else {
		return b;
	}
};

Abs : [a] => {
	if (a < 0) then {
		return -a;
	} else {
		return a;
	}
};	

Sum : [a, b] => {
	if (a = b) then {
		return 0;
	} else {
		return a + Sum(a + 1, b);
	}
};

Product : [a, b] => {
	if (a = b) then {
		return 1;
	} else {
		return a * Product(a + 1, b);
	}
};

Factorial : [a] => {
	if (a = 0) then {
		return 1;
	} else {
		return a * Factorial(a - 1);
	}
};

Fibonacci : [a] => {
	if (a < 2) then {
		return a;
	} else {
		return Fibonacci(a - 1) + Fibonacci(a - 2);
	}
};


